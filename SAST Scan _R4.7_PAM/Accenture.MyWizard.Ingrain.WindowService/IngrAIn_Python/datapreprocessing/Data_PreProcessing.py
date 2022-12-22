# -*- coding: utf-8 -*-
"""
Created on Thu Mar 21 14:24:47 2019

@author: sravan.kumar.tallozu
"""
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning)
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
import time
from SSAIutils import utils
#from datapreprocessing import MissingValuesImputer
#from datapreprocessing import DataEncoding
#from datapreprocessing import Data_Modification
#from datapreprocessing import Feature_Importance
#from datapreprocessing import aggregation
#from datapreprocessing import Add_Features
#from datapreprocessing import Textclassification
#from datapreprocessing import WoE_Binning
#from datapreprocessing import lessdatapoints
#import spacy
#import uuid
#import re
import datetime
#from pandas import Timestamp
#from datapreprocessing import Models_EstimatedRunTime
import sys
#import pickle
import pandas as pd
#from datapreprocessing import smote
import numpy as np
#from flask import jsonify
#import sklearn
#sys.modules['sklearn.preprocessing.label'] = sklearn.preprocessing._label
import base64
import json
import uuid
from SSAIutils import EncryptData
import platform
if platform.system() == 'Linux':
    thai_model_path='fasttext_model/cc.th.100.bin'
elif platform.system() == 'Windows':
    thai_model_path='fasttext_model\\cc.th.100.bin'
import fasttext,fasttext.util
def fetchProblemType(correlationId):
    dbconn,dbcollection = utils.open_dbconn("DE_DataCleanup")
    data_json = dbcollection.find({"CorrelationId" :correlationId})    
    dbconn.close()
    problemtype = data_json[0].get('Target_ProblemType')
    if problemtype == '1':
        problemtypef = 'Regression'
    elif problemtype == '2':
        problemtypef = 'Classification'
    elif problemtype == '3':
        problemtypef = 'Multi_Class'
    elif problemtype == '4':
        problemtypef = 'TimeSeries'
    return problemtypef

def checkFlagintemplate(templateusecaseid):
    dbconn,dbcollection = utils.open_dbconn("ME_FeatureSelection")
    data_json = dbcollection.find({"CorrelationId" :templateusecaseid}) 
    #print("data_json",data_json[0],templateusecaseid)
    if 'AllData_Flag' in data_json[0]:
        AllData_Flag = data_json[0].get('AllData_Flag')
    else:
        AllData_Flag = False
	
    return AllData_Flag

def update_AllData_Flag(correlationId,AllData_Flag):
    dbconn,dbcollection = utils.open_dbconn("ME_FeatureSelection")
    data_json = dbcollection.find({"CorrelationId" :correlationId}) 
    if 'AllData_Flag' in data_json[0]:
        dbcollection.update_one({"CorrelationId":correlationId},{'$set':{         
                                'AllData_Flag' : AllData_Flag
                               }}) 
    dbconn.close()
    return

def gettemplateproblemtype(templateusecaseid):
    dbconn,dbcollection = utils.open_dbconn("PS_BusinessProblem")
    data_json = dbcollection.find({"CorrelationId" :templateusecaseid}) 
    problemtype = data_json[0]['ProblemType']
    return problemtype

def UpdateRecommendedModels(correlationId,problemtype):
    dbconn,dbcollection = utils.open_dbconn("ME_RecommendedModels")
    data_json = dbcollection.find({"CorrelationId" :correlationId}) 
    SelectedModels = data_json[0]["SelectedModels"]
    key_list = []
    for key,value in SelectedModels.items():
        key_list.append(key)
        value["Train_model"] = "True"
    dbcollection.update_one({"CorrelationId":correlationId},{'$set':{         
                                'SelectedModels' : SelectedModels
                               }}) 
    dbconn.close()
    return key_list

'''
correlationId='8b7e05a1-912b-4249-9e6c-5ec1c129f039'
pageInfo='DataPreprocessing'
userId='abc@1234'
timeSeries=False
'''
def str_bool(s):
     if s=='True':
        return True
     if s=='False':
        return False
     else:
          raise ValueError

def main(correlationId,pageInfo,userId,flag = None):    
    try:
        start=time.time()
        min_df=utils.min_data()
        logger = utils.logger('Get',correlationId) 
        EnDeRequired = utils.getEncryptionFlag(correlationId)
        timeSeries = False                       
        #print("inside main")
        utils.updQdb(correlationId,'P','10',pageInfo,userId)  
        utils.logger(logger,correlationId,'INFO',('Data Preprocessing: '+" Process initiated at : "+str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        
        '''        
        count = utils.check_CorId("ME_FeatureSelection",correlationId,pageInfo)
        if count >= 1:
            utils.updQdb(correlationId,'C','100',pageInfo,userId)          
            return'''
            
        
        dbconn,dbcollection = utils.open_dbconn("PS_BusinessProblem")
        data_json = dbcollection.find({"CorrelationId" :correlationId})    
        dbconn.close()
        if data_json[0]['ProblemType']!="TimeSeries":
            input_columns = data_json[0].get('InputColumns')
        else:
            timeSeries = True
            input_columns = [data_json[0]['TimeSeries']['TimeSeriesColumn']]
        target_variable = data_json[0].get('TargetColumn')
        uniqueIdentifier = data_json[0].get('TargetUniqueIdentifier')
        #print (uniqueIdentifier)
        # Fetch data
        if uniqueIdentifier!=None:
            data_cols_t = input_columns + [target_variable]+[uniqueIdentifier]
        else:
            data_cols_t = input_columns + [target_variable]
        offlineutility = utils.checkofflineutility(correlationId)
        if offlineutility:
            data = utils.data_from_chunks_offline_utility(corid=correlationId, collection="DataSet_IngestData",lime=None, recent=None,DataSetUId=offlineutility)
        else:
            data = utils.data_from_chunks(corid=correlationId,collection="PS_IngestedData")
        if data.shape[0]<20 and data_json[0]['ProblemType']!="TimeSeries" and "InstaId" not in data_json[0]:
            from datapreprocessing import lessdatapoints
            data= lessdatapoints.minimaldatapoints(data,uniqueIdentifier)  
        #print ("abcd",data.shape)
        #print (data)        
        #print(data[target_variable].unique())		
        features_created = []
        utils.logger(logger,correlationId,'INFO',('Add new feature Started at : '+str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        try:
            dbconn,dbcollection = utils.open_dbconn("DE_AddNewFeature")
            data_json = dbcollection.find({"CorrelationId" :correlationId})    
            dbconn.close()
            if 'Features_Created' in data_json[0] and  len(data_json[0]['Features_Created']) >0:
                data_new_feature = utils.data_from_chunks(corid=correlationId,collection="DE_NewFeatureData") 
                data_new_feature = data_new_feature[data_json[0]['Features_Created']]
                if len(set(data.columns).intersection(set(data_json[0]['Features_Created'])))>0:
                    data.drop(list(set(data.columns).intersection(set(data_json[0]['Features_Created']))),axis=1,inplace=True) 
                data = pd.concat([data,data_new_feature,],axis=1)
                data_cols_t = set(data_cols_t+data_json[0]['Features_Created'])
                features_created = data_json[0]['Features_Created']
        except Exception:
            utils.logger(logger,correlationId,'INFO',("Add New Features in data_preprocessing"),str(None))
        data = data[data_cols_t]
        data_cols = data.columns        
        data_dtypes = dict(data.dtypes)
        ndata_cols = data.shape[1]
        datasize = (sys.getsizeof(data)/ 1024) / 1024               
        #print (data_cols)
        #Fetch user selected data types
        dbconn,dbcollection = utils.open_dbconn("DE_DataCleanup")
        features_data_json = dbcollection.find({"CorrelationId" :correlationId}) 
        dbconn.close()
        Features = features_data_json[0].get('Feature Name')
        try:
            Features_new = features_data_json[0].get('Feature Name_New Feature')
        except Exception:
            utils.logger(logger,correlationId,'INFO',("Decryption of Feature from DE_DataCleanup not needed/ends"),str(None))                
        if EnDeRequired :
            t = base64.b64decode(Features) #DE55......................................
            Features = json.loads(EncryptData.DescryptIt(t))
            try:
                t = base64.b64decode(Features_new) #DE55......................................
                Features_new = json.loads(EncryptData.DescryptIt(t))
            except Exception:
                utils.logger(logger,correlationId,'INFO',("Decryption of Feature from DE_DataCleanup not needed/ends"),str(None))
        Features_data = Features     
        try:
            #print (Features,'2323', Features_new)
            Features = {**Features, **Features_new}
        except Exception:
            utils.logger(logger,correlationId,'INFO',("Decryption of Feature from DE_DataCleanup not needed/ends"),str(None))
        utils.logger(logger,correlationId,'INFO',('Add new feature Completed at : '+str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        #print (Features.keys(),data_cols_t,data.columns)
        #Removing Correlated columns
        utils.updQdb(correlationId,'P','15',pageInfo,userId)
        try:
            datacols_F=list(Features.keys())
            if uniqueIdentifier!=None and uniqueIdentifier not in datacols_F:
                datacols_F.append(uniqueIdentifier)
            data = data[datacols_F]
            data_cols = data.columns        
            data_dtypes = dict(data.dtypes)
            ndata_cols = data.shape[1]
            datasize = (sys.getsizeof(data)/ 1024) / 1024 
        except:
            utils.logger(logger,correlationId,'INFO',("Features keys and values"),str(None))
        #print(target_variable,data.columns)
        #print("inside main")
        # Fetch the changed data type
        features_dtypesf = {}
        for key,value in Features.items():        
            d_type = [key1 for key1,value1 in value.get('Datatype').items() if value1 =='True']
            if d_type:    
                features_dtypesf.update({key:d_type[0]})
            else:
                features_dtypesf.update({key:'ND'})
        #print ("abcd",features_dtypesf)
        if not timeSeries: 
            features_dtypesf[uniqueIdentifier] = 'object'
        #print ("abcd",features_dtypesf)
        #Throw category type not assigned if ND,
        
        '''
        category_cols=[]
        for x, y in features_dtypesf.items():
            if y=='category':
                category_cols.append(x)
        
       
        for c in category_cols:
             #data[c] = data[c].replace(d1,'_',regex=True)
             data[c] = data[c].replace('[^a-zA-Z0-9 ]', '_', regex = True) '''
        #print ("hereee",features_dtypesf)
        # Change the data type
        cate = ['category']
        inte = ['float64', 'int64']
        try:
            for key4,value4 in features_dtypesf.items():            
                if (data_dtypes.get(key4)).name in ['float64','int64']:
                    if value4 in cate:
                        data[key4] = data[key4].astype(str)                      
                elif (data_dtypes.get(key4)).name == 'object':  
                    if value4 == inte[1]:   
                        data[key4] = data[key4].astype(int)
                    elif value4 == inte[0]:
                        data[key4] = data[key4].astype('float64')
                    utils.logger(logger,correlationId,'INFO',("features datatype"),str(None))
                elif (data_dtypes.get(key4)).name =='bool':
                    data[key4] = data[key4].astype(str)
        except Exception:
            raise Exception("Datatype selection not proper for column '{}'".format(key4))
               
        #print("insida Done")
        #print (features_dtypesf)
        # Remove Id and Text columns
        '''CHANGES STARTED'''
        #problemtypef=fetchProblemType(correlationId)
        if not timeSeries:
            data[uniqueIdentifier] = data[uniqueIdentifier].astype('object')
      
        drop_cols_t=[]   
        text_list = []
        date_cols=[]
        for key5,value5 in features_dtypesf.items():
            if not timeSeries:            
                if value5 in ['Id','Text','datetime64[ns]','datetime64[ns, UTC]']:
                    if key5 != uniqueIdentifier:
                        drop_cols_t.append(key5)
                if value5 == 'Text':
                    text_list.append(key5)
                if value5 in ['datetime64[ns]','datetime64[ns, UTC]']:
                    date_cols.append(key5)
            if timeSeries:
                if value5 in ['Id']:
                    drop_cols_t.append(key5)
            #if problemtypef=='Text_Classification':
            #    if value5 in ['Id','datetime64[ns]']:
            #        drop_cols_t.append(key5)
        drop_cols_t= list(set(drop_cols_t)- set(text_list)-set(date_cols))
        #if not timeSeries:
        #    for cols_dates in date_cols:
        #        #data[cols_dates] =  pd.to_datetime(data[cols_dates], format='YYYY-MM-DD HH:MM:SS')
        #        #data[cols_dates] = data[cols_dates].dt.strftime("YYYY-MM-DD HH:MM:SS")
        #        data[cols_dates] = pd.to_datetime(data[cols_dates],dayfirst=True)
        #        data = data[data[cols_dates].notnull()]
        #        data.reset_index(inplace = True, drop=True)
        '''CHANGES ENDED'''
        #print ("before removal",data.columns)        
        if drop_cols_t:
            data.drop(drop_cols_t,axis=1,inplace=True)
        data_cols = data.columns
        #print (data_cols)
        #Fetch Preprocessing details 
        dbproconn,dbprocollection = utils.open_dbconn("DE_DataProcessing")
        data_json = list(dbprocollection.find({"CorrelationId" :correlationId}))   #DE.....................................
        if EnDeRequired :
             t = base64.b64decode(data_json[0].get('MissingValues'))
             data_json[0]['MissingValues']     =  json.loads(EncryptData.DescryptIt(t))
             t = base64.b64decode(data_json[0].get('DataModification'))
             data_json[0]['DataModification']  =  json.loads(EncryptData.DescryptIt(t))
             t = base64.b64decode(data_json[0].get('Filters'))
             data_json[0]['Filters']           =  json.loads(EncryptData.DescryptIt(t))  
        
        DataProCollectionId = data_json[0].get('_id')
        utils.updQdb(correlationId,'P','20',pageInfo,userId)
        #Smote Flag  
        from datapreprocessing import smote		
        if timeSeries:
            smote_F = False
        try:
            smote_F = data_json[0].get('Smote').get('Flag')
        except:
            smote_F=False
        
        
        #Fetch Missing data
        Missing_details=data_json[0].get('MissingValues')
        
        utils.logger(logger,correlationId,'INFO',('Missing values imputer started at : '+str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
     
        
        #Missing Value Imputer
        for key,value in Missing_details.items():            
            if value.get('ChangeRequest') == 'True' and value.get('PChangeRequest') != 'True':
                method=None
                if value.get('CustomFlag')== None or value.get('CustomFlag')!= "True":
                    for key1,value1 in value.items():     
                                                     
                        if key1 != 'ChangeRequest' and key1!='PChangeRequest' and key1!='CustomFlag':
                            if value1 == 'True':
                                method=None
                                constant=None
                                if key1 in ['Mean', 'Median', 'Mode']:                        
                                    if key1 == 'Mode':
                                        method = 'most_frequent'
                                        break
                                    else:
                                        method = key1.lower() 
                                        break
                                else:
                                    method = 'constant'
                                    constant=key1
                                    break
                            else:
                                if key1=='CustomValue' and value1!='False':
                                    method = 'constant'
                                    constant=value1
                                    #if (data[key1]==-99999.007).all():
                                    #    data[key1]=[np.nan]*data.shape[0]	
                                    break
                else:
                     method = 'constant'
                     constant = value.get('CustomValue')                   
                            #constant=re.sub(r"[^a-zA-Z0-9 ]", '_', key1)
                if method!=None or (method=='constant' and constant!=None) :     
                    from datapreprocessing import MissingValuesImputer				
                    data = MissingValuesImputer.data_imputer(data,method,key,constant)
                    value['PChangeRequest'] = "True"

#                    keyupd = "MissingValues."+str(key)+(".PChangeRequest")
#                    dbprocollection.update_many({'_id':DataProCollectionId},{'$set':{         
#                                keyupd : "True"                                                      
#                               }}) 
#        print("outliers Done")
        #print (data)
        #Fetch Filter data
        utils.updQdb(correlationId,'P','30',pageInfo,userId)
        utils.logger(logger,correlationId,'INFO',('Missing values imputer completed and filters started at : '+str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))

        filters=data_json[0].get('Filters')
        for key,value in filters.items():        
            if value.get('ChangeRequest') == 'True' and value.get('PChangeRequest') != 'True':            
                values_t = [key1 for key1,value1 in value.items() if value1=='True'and (key1 != 'ChangeRequest' and key1!='PChangeRequest')]             
                if len(values_t):                  
                    data= data.loc[data[key].isin(values_t)]
                    value['PChangeRequest'] = "True"
#                    keyupd = "Filters."+str(key)+(".PChangeRequest")
#                    dbprocollection.update_many({'_id':DataProCollectionId},{'$set':{         
#                                    keyupd : "True"                                                      
#                                   }})
        
        #print(data.columns)
        #data = data[data[target_variable]!='']
        data_unique_vals=list(data[target_variable].dropna().unique())
        #print("data_unique_vals",data_unique_vals)
        data_unique_vals=[str(i) for i in data_unique_vals]
    
        if data.shape[0] == 0:
            raise Exception("No data for preprocessing, applied Filter")
            #utils.updQdb(correlationId,'E','No data for preprocessing, applied Filter',pageInfo,userId)            
            #return
        utils.logger(logger,correlationId,'INFO',('filters completed and modification started at : '+str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        
        #Data Modification
        #Fetch Data Modification data
        Features = data_json[0].get('DataModification',{}).get('Features')
        Outliers = []
        data_transformations = []
        for key,value in Features.items():
            if key !="Interpolation":
                Outliers_data = value.get('Outlier')
                if Outliers_data:
                    if Outliers_data.get('CustomValue')=='':
                        value_t = [key1 for key1,value1 in Outliers_data.items() if value1=='True' and (Outliers_data.get('ChangeRequest') == 'True' and Outliers_data.get('PChangeRequest') != 'True') and (key1 != 'ChangeRequest' and key1!='PChangeRequest')]
                    else:
                        #print("elsepart")
                        value_t=int(Outliers_data.get('CustomValue'))
                    if value_t:
                        if type(value_t)==(int or float):
                            Outliers.append({key:value_t})
                        else:
                            Outliers.append({key:value_t[0]}) 
                data_transformations_data = value.get('Skewness')
                if data_transformations_data:
                    value_t1 = [key2 for key2,value2 in data_transformations_data.items() if value2=='True' and (data_transformations_data.get('ChangeRequest') == 'True' and data_transformations_data.get('PChangeRequest') != 'True') and (key2 != 'ChangeRequest' and key2!='PChangeRequest')]
                    if value_t1:
                        data_transformations.append({key:value_t1[0]})
        
        if timeSeries:
                interapolationTechnique = data_json[0]['DataModification']['Features']['Interpolation']
        

         # Outlier Treatment
        for val in range(len(Outliers)):  
            from datapreprocessing import Data_Modification		
            Outlier_col = list(Outliers[val].keys())[0]
            skewed_data = ({Outlier_col :Features_data[Outlier_col]['Skewness']})
            skewedflag = list(skewed_data.values())[0]
            wtd         = list(Outliers[val].values())[0]              
            data = Data_Modification.remove_outlier(skewedflag,data,Outlier_col,wtd)
            Features[Outlier_col]["Outlier"]["PChangeRequest"] = "True"
            
#            keyupd = "DataModification.Features."+str(Outlier_col)+".Outlier"".PChangeRequest"
#            dbprocollection.update_many({'_id':DataProCollectionId},{'$set':{         
#                                       keyupd : "True"                                                      
#                                        }})
        if not timeSeries:  
            from datapreprocessing import Data_Modification		
            # Data Transformations
            for val in range(len(data_transformations)):             
                trans_column = list(data_transformations[val].keys())[0]
                wtd          = list(data_transformations[val].values())[0]
                if wtd == 'BoxCox':
                    wtd = 'BoxCox'
                elif wtd == 'Reciprocal':
                    wtd = 'Reciprocal'             
                elif wtd == 'Standardization':
                    wtd = 'StandardScaler'
                elif wtd == 'Normalization':
                    wtd = 'Normalizer'                    
                if wtd in ['StandardScaler','MinMaxScaler','RobustScaler','Normalizer','BoxCox','Reciprocal']:                
                    data,scaler = Data_Modification.scaler(data,trans_column,wtd)                
                    utils.save_file(scaler,str(str(wtd)+'-'+str(trans_column)),'None',correlationId,pageInfo,userId,FileType=wtd)
                    Features[trans_column]["Outlier"]["PChangeRequest"] = "True"
#                    keyupd = "DataModification.Features."+str(trans_column)+".Outlier"".PChangeRequest"
#                    dbprocollection.update_many({'_id':DataProCollectionId},{'$set':{         
#                                           keyupd : "True"                                                      
#                                            }})
                if wtd == 'Log':                 
                    data = Data_Modification.Log_Trans(data,trans_column)
                    Features[trans_column]["Outlier"]["PChangeRequest"] = "True"
#                    keyupd = "DataModification.Features."+str(trans_column)+".Outlier"".PChangeRequest"
#                    dbprocollection.update_many({'_id':DataProCollectionId},{'$set':{         
#                                           keyupd : "True"                                                      
#                                            }})
        
                
        #After data transformation we are getting Nan's, WHY?
        if data.isnull().values.any():
            data = data.dropna(axis=0)
        utils.updQdb(correlationId,'P','40',pageInfo,userId)
        #Binning data        
        binning_data= data_json[0].get('DataModification',{}).get('ColumnBinning')
        if binning_data:
            temp = {}
            for  keys,values in binning_data.items():                                            
                if values.get('ChangeRequest',{}).get('ChangeRequest') == 'True' and values.get('PChangeRequest',{}).get('PChangeRequest') != 'True':                                        
                    for keys1,values1 in values.items():  
                        if values1.get('Binning') == 'True':
                            Ncat = values1.get('NewName') 
                            subcat =  values1.get('SubCatName')
                            temp.update({subcat:Ncat})
                    Ucat = list(set(temp.values()))                       
                    for indx in range(len(Ucat)):
                        column = str(keys)            
                        subcat = [key2 for key2,value2 in temp.items() if value2 == Ucat[indx]]                        
                        data.loc[data[column].isin(subcat),column]=Ucat[indx]
                    temp = {}
#                    binning_data["ColumnBinning"][column]["PChangeRequest"]["PChangeRequest"] = "True"
#                    keyupd = "DataModification.ColumnBinning."+str(column)+".PChangeRequest"+".PChangeRequest"
#                    dbprocollection.update_many({'_id':DataProCollectionId},{'$set':{         
#                                       keyupd : "True"                                                      
#                                        }})
        
        '''AUTOBINNING CHANGES START HERE'''
        #get the autobinning data, if present
        autobinning_data = data_json[0].get('DataModification',{}).get('AutoBinning')
        
        #perform autobinning
        bins_dict = {}
        autobinned_cols = []
        if(autobinning_data!=None):
            autobinned_cols = [key for key in autobinning_data if autobinning_data[key]=='True']
            if (autobinned_cols!=[]):
                from datapreprocessing import WoE_Binning
                dbconn,dbcollection = utils.open_dbconn("DE_DataCleanup")
                features_data_json = dbcollection.find({"CorrelationId" :correlationId}) 
                dbconn.close()
                Features = features_data_json[0].get('Feature Name')
                if EnDeRequired :
                    t = base64.b64decode(Features) #DE55......................................
                    Features = json.loads(EncryptData.DescryptIt(t))
                cat_cols = [key for key in Features if key in autobinned_cols and Features[key]["Datatype"]["category"]=='True']
                numerical_cols = list(set(autobinned_cols) - set(cat_cols))
                bins_dict, data = WoE_Binning.woe(data, target_variable, 10, cat_cols, numerical_cols)
        
        '''AUTOBINNING CHANGES END HERE'''
        
        '''ADD NEW FEATURES CHANGES START HERE'''
        '''
        #print("adding new features") 
        map_encode_new_feature={}
        features_created = []
        feature_not_created ={}
        value_store= {}
        if not timeSeries:
            feature_mapping=data_json[0].get('DataModification',{}).get('NewAddFeatures')
            if feature_mapping != None and str(feature_mapping)!='' and len(feature_mapping) >=1:
                from datapreprocessing import Add_Features
                data,feature_not_created, value_store = Add_Features.add_new_features(data,feature_mapping,pageInfo="DataPreprocessing")
                if len(feature_mapping) == len(feature_not_created):
                    #print("No Features were created")
                else:
                    features_created = list(set(list(feature_mapping.keys())) - set(feature_not_created.keys()))
                    data = data.dropna()
                    
                    for key in list(feature_mapping.keys()):
                        #print("key:", key)
                        #print(data[key])
                        if key in features_created:
                            if data.dtypes[key] not in ['int64','float64']:
                                map_encode_new_feature[key] = {'attribute': 'Nominal','encoding': 'Label Encoding','ChangeRequest': 'True','PChangeRequest': 'False'}
                                if data.dtypes[key] =='bool':
                                    data[key] = data[key].astype(str)
        
        #add data to DE_Datacleanup Filtered data for teach and test
        if len(features_created)>0:
            dbproconn,dbprocollection = utils.open_dbconn("DataCleanUP_FilteredData")
            data_json1 = dbprocollection.find({"CorrelationId" :correlationId})  
            CollectionId = data_json1[0].get('_id')
            unique_values_from_db=data_json1[0].get('ColumnUniqueValues')
            if EnDeRequired :
                t = base64.b64decode(unique_values_from_db)     #DE55......................................
                unique_values_from_db = json.loads(EncryptData.DescryptIt(t))
            column_data_types=data_json1[0].get('types')
            for feature in features_created:
                data_unique_value=list(data[feature].unique())
                unique_values_from_db[feature]=np.array(data_unique_value).tolist()
                column_type = data.dtypes[feature]
                if column_type == 'O':
                    column_data_types[feature] = 'category'
                elif column_type == 'float64' or column_type == 'int64':
                    column_data_types[feature] = 'float64'
            if EnDeRequired :        
                unique_values_from_db = EncryptData.EncryptIt(json.dumps(unique_values_from_db))
            dbprocollection.update_many({'_id':CollectionId},{'$set':{         
                                      'ColumnUniqueValues' : unique_values_from_db}}) 
            dbprocollection.update_many({'_id':CollectionId},{'$set':{         
                                      'types' : column_data_types}}) 
        '''
        '''ADD FEATURES CHANGES END HERE'''
        
        '''TP CHANGES START'''
        utils.updQdb(correlationId,'P','45',pageInfo,userId)
        utils.logger(logger,correlationId,'INFO',('Modification completed and Text Preprocessing started at : '+str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))

        dbconn,dbcollection = utils.open_dbconn("SSAI_DeployedModels")
        data_json1 = dbcollection.find({"CorrelationId" :correlationId})    
        dbconn.close()
        try:
            language = data_json1[0].get('Language').lower()
        except Exception:
            language = 'english'
        text_dict= data_json[0].get('DataModification',{}).get('TextDataPreprocessing')
        drop_text_cols = ''
        freq_dict ={}
        
        '''CLUSTERING CHANGES START'''
        n_cluster = 1
        n_gram = None
        clustering_flag = True
        aggregation_selected = None
																																																																					  
        #print ("abcd",data.shape)
        if text_list !=[] and text_dict !=None and text_dict.get('TextColumnsDeletedByUser') is not None:
            
            '''CLUSTERING HANGES END'''
            text_cols_drop=text_dict.get('TextColumnsDeletedByUser')
																																 
		   
            if set(text_list) != set(text_cols_drop):
                n_cluster = int(text_dict.get('NumberOfCluster'))
                n_gram = tuple(text_dict.get('N-grams'))
                try:
                    clustering_flag = eval(text_dict.get('Clustering'))
                except:
                    clustering_flag=False
                if not clustering_flag:
                    from datapreprocessing import aggregation
                    aggregation_selected = text_dict.get('Aggregation')
                #data.drop(text_cols_drop,axis=1,inplace=True)
            #data_text_df =pd.dataframe
                text_list = list(set(text_list)-set(text_cols_drop))
                if text_cols_drop!=text_list:
                    if len(text_list)!=0:
                        data_text_df=data[text_list]
                        for col in text_list:
                            data_text_df[col] = data_text_df[col].astype(str)
                    for col in text_list:
                        #if x in text_list:
                        dict_feature={}
                        if int(text_dict.get(col).get('Most_Frequent'))!=0:
                            freq_most_value = len(pd.Series(' '.join(data_text_df[col]).split()).value_counts())*int(text_dict.get(col).get('Most_Frequent'))/100
                            #x= pd.Series(' '.join(data_text_df[col]).split()).value_counts()
                            freq_most = pd.Series(' '.join(data_text_df[col]).split()).value_counts()[:int(freq_most_value)] #remove most frequent words
                            freq_most = list(freq_most.index)
                            data_text_df[col]=utils.lambda_exec(data_text_df[col],freq_most)
                            dict_feature['freq_most'] = freq_most
                        else:
                            dict_feature['freq_most'] = {}
                        if int(text_dict.get(col).get('Least_Frequent'))!=0:
                            freq_rare_value = len(pd.Series(' '.join(data_text_df[col]).split()).value_counts())*int(text_dict.get(col).get('Least_Frequent'))/100
                            #y=pd.Series(' '.join(data_text_df[col]).split()).value_counts()
                            freq_rare = pd.Series(' '.join(data_text_df[col]).split()).value_counts()[-(int(freq_rare_value)):]  #remove least frequent words
                            freq_rare = list(freq_rare.index) #list of those words
                            data_text_df[col] = utils.lambda_exec(data_text_df[col],freq_rare)
                            dict_feature['freq_rare'] = freq_rare
                        else:
                            dict_feature['freq_rare'] = {}
                        freq_dict[col] = dict_feature
                    # dbconnection
                    '''text_dict= {
                    		"region": {
                    			"Lemmitize": "True",
                    			"Pos": "True",
                    			"Stemming": "True",
                                "Stopwords":["Harsh","Hello"],
                                "Most_Frequent": "10",
                                "Least_Frequent": "5",
                    		},
                    		"smoker": {
                    			"Lemmitize": "True",
                    			"Pos": "True",
                    			"Ptemming": "True",
                                "Stopwords":["Harsh"],
                                "Most_Frequent": {"Count":10},
                                "Least_Frequent": {"Count":10}
                    		},
                    		"sex": {
                    			"Lemmitize": "True",
                    			"Pos": "True",
                    			"Stemming": "True",
                                "Stopwords":[],
                                "Most_Frequent": {"Count":0},
                                "Least_Frequent": {"Count":0}},
                            "Feature_Generator":"Count_Vectorizer",
                             "N-grams":[1,2],
                             "TextColumnsDeletedByUser":['sex']
                            }'''
                    ''' GET FLAGS FROM DB'''
                    
                    for key5,value5 in text_dict.items():
                        from datapreprocessing import aggregation
                        '''CLUSTERING CHANGES START'''
                        if key5 not in ["Feature_Generator","N-grams","TextColumnsDeletedByUser","NumberOfCluster","Clustering","Aggregation"]:
                            '''CLUSTERING CHANGES END'''
                            for key6,value6 in value5.items():
                            #print('Key5',key6)
                            #print('value',value6)
                                if value6 in ['True','False']:
                                    text_dict[key5][key6] = str_bool(value6)
                    
                    
                    
                    '''WRITE CODE IN A LOOP WHEN DATA FLAGS COME FROM THE DB SINCE LEMMITIZE STEMMING IS USER DEPENDENT ''' 
                    for x in text_list:
                        from datapreprocessing import Textclassification
                        import spacy
                        #new_df=data[target_variable]
                        #new_df=new_df.apply(Textclassification.text_process,args=[text_dict[x]['pos'],text_dict[x]['Lemmitize'],text_dict[x]['stemming']])
                        '''Please write the script for combining all the text columns together below this line and before Data Encoding Starts'''
                        #print("PRINT!!!!!:","data_text_df.{}.apply(Textclassification.text_process(pos=text_dict[x]['pos'],lemmetize=text_dict[x]['Lemmitize'],stem=text_dict[x]['stemming']))".format(x))
                        #data_text_df[x]=eval("data_text_df.{}.apply(Textclassification.text_process(pos=text_dict[x]['pos'],lemmetize=text_dict[x]['Lemmitize'],stem=text_dict[x]['stemming']))".format(x))
                        
                        if language =='english':
                            spacy_vectors = spacy.load('en_core_web_lg')
                        elif language =='spanish':
                            spacy_vectors = spacy.load('es_core_news_lg')
                        elif language =='portuguese':
                            spacy_vectors = spacy.load('pt_core_news_lg')
                        elif language =='german':
                            spacy_vectors = spacy.load('de_core_news_lg')
                        elif language =='chinese':
                            spacy_vectors = spacy.load('zh_core_web_lg')
                        elif language =='japanese':
                            spacy_vectors = spacy.load('ja_core_news_lg')
                        elif language =='french':
                            spacy_vectors = spacy.load('fr_core_news_lg')
                        elif language =='thai':
                            spacy_vectors = []
                        data_text_df[x] = data_text_df[x].apply(Textclassification.text_process1,args=[True,text_dict[x]['Lemmitize'],text_dict[x]['Stemming'],text_dict[x]['Stopwords'],language,spacy_vectors])
                    '''Please write the script for combining all the text columns together below this line and before Data Encoding Starts'''
                    data_text_df.replace(r'^\s*$',np.nan,regex=True,inplace=True)
                    #null_values_df=data_text_df.isnull().mean()*100
                    #null_values_df_20=null_values_df[null_values_df>20]
                    drop_text_cols=[]
                    
                    if drop_text_cols:
                        data_text_df.drop(drop_text_cols,axis=1,inplace=True)
                    for key5,value5 in text_dict.items():
                        from datapreprocessing import aggregation
                        if key5 not in ["Feature_Generator","N-grams","TextColumnsDeletedByUser","NumberOfCluster","Clustering","Aggregation"]:
                            for key6,value6 in value5.items():
                            #print('Key5',key6)
                            #print('value',value6)
                                if value6 in [True,False]:
                                    text_dict[key5][key6] = str(value6)
                    '''TP CHANGES END'''
            else:
                raise Exception("Please complete and review Pre-Processing steps for Text Attributes under Text Data Pre-Processing Section.")
        #print ("defg",data.shape)
        utils.logger(logger,correlationId,'INFO',('Text Preprocessing completed and Encoding Started at : '+str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        
        utils.updQdb(correlationId,'P','55',pageInfo,userId)
        #print("Encoding ")
        #Fetch Data Encoding
        from datapreprocessing import DataEncoding
        Data_to_Encode=data_json[0].get('DataEncoding') 
        '''CHANGES START HERE'''
        #if len(map_encode_new_feature)>0:
        #    Data_to_Encode.update(map_encode_new_feature)
            
                
        '''CHANGES END HERE'''
        OHEcols = []
        LEcols = []
        for keys,values in Data_to_Encode.items():                
            if values.get('encoding') == 'One Hot Encoding' and (values.get('ChangeRequest') == 'True' and values.get('PChangeRequest') != 'True') and (keys != 'ChangeRequest' and keys!='PChangeRequest'):
                OHEcols.append(keys)
            elif values.get('encoding') == 'Label Encoding' and (values.get('ChangeRequest') == 'True' and values.get('PChangeRequest') != 'True') and (keys != 'ChangeRequest' and keys!='PChangeRequest'):
                LEcols.append(keys)
                
        '''AUTOBINNING CHANGES START HERE'''
        #for the columns which  are autobinned, do not perform any kind of encoding
        if (autobinned_cols!=[]):
            OHEcols = list(set(OHEcols) - set(autobinned_cols))
            LEcols = list(set(LEcols) - set(autobinned_cols))
        '''AUTOBINNING CHANGES END HERE'''
        
        
        if len(OHEcols):   
            from datapreprocessing import DataEncoding		
            data,_,ohem,enc_cols=DataEncoding.one_hot(data, OHEcols)
            del _
            utils.save_file(ohem,'OHE',enc_cols,correlationId,pageInfo,userId,FileType='OHE')
            for i in range(len(OHEcols)):                
                keyupd = "DataEncoding."+str(OHEcols[i])+(".PChangeRequest")
                #dbprocollection.update_many({'_id':DataProCollectionId},{'$set':{ keyupd : "True"}})
        if len(LEcols):
            from datapreprocessing import DataEncoding
            data,_,lencm,Lenc_cols = DataEncoding.Label_Encode(data, LEcols)
            del _
            utils.save_file(lencm,'LE',Lenc_cols,correlationId,pageInfo,userId,FileType='LE')
            for i in range(len(LEcols)):
                keyupd = "DataEncoding."+str(LEcols[i])+(".PChangeRequest")
                #dbprocollection.update_many({'_id':DataProCollectionId},{'$set':{keyupd : "True"}})

        #if len(map_encode_new_feature)>0:
        #    for item in list(map_encode_new_feature.keys()):
        #        del Data_to_Encode[item]
        utils.logger(logger,correlationId,'INFO',('Encoding Completed at : '+str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))

        if EnDeRequired :
             t = data_json[0].get('MissingValues')
             data_json[0]['MissingValues']     = EncryptData.EncryptIt(json.dumps(t))
             t = data_json[0].get('DataModification')
             data_json[0]['DataModification']  =  EncryptData.EncryptIt(json.dumps(t))
             t = data_json[0].get('Filters')
             data_json[0]['Filters']           =  EncryptData.EncryptIt(json.dumps(t))
             
        changeJson = data_json[0]
        dbconn,dbcollection = utils.open_dbconn('DE_DataProcessing')
        dbcollection.update_many({"CorrelationId"     : correlationId},
                                  { "$set":changeJson},upsert=True)
        
        drop_cols = OHEcols + LEcols
        if drop_cols:
            data_t = data.drop(drop_cols,axis=1)
        else:
            data_t = data
        
        del data
        utils.updQdb(correlationId,'P','60',pageInfo,userId)
        #print("Encoding Done")
        if not timeSeries:
            #temp_col=[data_t.columns[i] for i in range(0,data_t.columns.__len__()) if data_t.columns[i] in ["datetime64[ns]","object"]]
            #temp_col = data_t.columns[(data_t.dtypes == 'datetime64[ns]') or (data_t.columns[data_t.dtypes == 'object'])]
            temp_col1 = list(data_t.columns[(data_t.dtypes == 'datetime64[ns]')])
            temp_col2 = list(set(list(data_t.columns[(data_t.dtypes == 'object')]))-set(text_list))
            temp_col=list(set(temp_col1+temp_col2+date_cols))
            #print(temp_col,"tempcol")
            if uniqueIdentifier in temp_col:
                temp_col.remove(uniqueIdentifier)
            #temp_col = (data_t.columns[data_t.dtypes == 'datetime64[ns]']) or (data_t.columns[data_t.dtypes == 'object'])
            if len(temp_col) != 0: 
                data_t = data_t.drop(temp_col,axis=1)
    #            utils.updQdb(correlationId,'E','Their are strings in data, preprocess data, Data Encoding',pageInfo,userId)                            
                
          
                
            # Validate their are no string values and Nulls in data 
            if data_t.isnull().values.any():
                from flask import jsonify
                utils.logger(logger,correlationId,'INFO', ('data dtypes'+str(dict(data_t.isna().sum()))),str(None))

                raise Exception("Their are nulls in data, preprocess data, Missing values")
                #utils.updQdb(correlationId,'E',"Their are nulls in data, preprocess data, Missing values",pageInfo,userId)  
                #return
    #            return jsonify({'status': 'false', 'message':'Preprocessing'}), 200
                                
            if ('object') in list(set(list(data_t.columns[(data_t.dtypes == 'object')]))-set(text_list)) or ('datetime64[ns]') in list(data_t.dtypes):
                from flask import jsonify
                data_tdtypes = dict(data_t.dtypes)
                utils.logger(logger,correlationId,'INFO', ('data dtypes'+str(data_tdtypes)),str(None))
                raise Exception("Their are strings/datatime stamps in data, preprocess data, Data Encoding")
                #utils.updQdb(correlationId,'E',"Their are strings/datatime stamps in data, preprocess data, Data Encoding",pageInfo,userId)                
                #return
#            return jsonify({'status': 'false', 'message':'Preprocessing'}), 200      
        else:
            temp_col=[]
        
        '''TP CHANGES START'''
        problemtypef=fetchProblemType(correlationId)
        problemtypeflag = False
        if len(text_list)!=0 and  (set(text_list) != set(text_cols_drop)) and len(data_text_df.columns)!=0 :
                problemtypeflag =True
        
        '''TP CHANGES END''' 
        #print("Encoding Done2")
        data_t=data_t[~data_t.isin([np.nan, np.inf, -np.inf]).any(1)].reset_index(drop=True)
        '''CHANGES STARTED'''
        if problemtypeflag and len(data_text_df.columns)!=0:

            #data_t.drop(data_text_df.columns,inplace=True,axis=1)
            #data_t=pd.concat([data_t,data_text_df],axis=1)
            #data_t.dropna(inplace=True)
            data_text_df.reset_index(inplace=True,drop=True)
            data_t['All_Text'] = data_text_df[list(set(data_t.columns) and set(data_text_df.columns))].astype(str).apply(' '.join, axis=1)
            #final_cols=list(data_t.columns).append('All_Text')
            data_t.drop(list(set(data_t.columns) and set(list(data_text_df.columns)+drop_text_cols)),inplace=True,axis=1)
            #data_t=data_t[data_t['All_Text']!='nan']
            #data_t=data_t.reset_index(drop=True)
        '''CHANGES ENDED'''  
        '''CLUSTERING CHANGES START'''
        utils.logger(logger,correlationId,'INFO',('Clustering Started at : '+str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))

        cluster_columns=[]
        silhouette_graph = ''
        if problemtypeflag and clustering_flag:
            from datapreprocessing import Textclassification
            
            cluster_df,optimum_cluster, silhouette_graph = Textclassification.clustering(data_t['All_Text'],n_cluster,correlationId,language=language)
            cluster_columns = list(cluster_df.columns)
            #data_t = pd.concat([data_t,cluster_df,],axis=1)
            sum_cluster = pd.DataFrame(cluster_df.sum(axis=1),columns=['cluster_sum'])
            index_list = list(np.where(sum_cluster['cluster_sum']==0)[0])

            data_t = pd.concat([data_t,cluster_df,],axis=1)
            data_t.drop(data_t.index[index_list],inplace=True)
            data_t = data_t.reset_index(drop=True)
        elif problemtypeflag and not clustering_flag:
            flag_ngram, indexes_to_drop = Textclassification.check_ngram(data_t['All_Text'],n_gram)
            if flag_ngram:
                n_gram = (1.0, 1.0)
                flag_ngram_2, indexes_to_drop_2 = Textclassification.check_ngram(data_t['All_Text'],n_gram)
                if flag_ngram_2:
                    raise Exception("N-gram range selected is higher than number of words present in the text columns. Please lower n-gram range")
                else:
                    if len(indexes_to_drop_2)!=0:
                        data_t.drop(data_t.index[indexes_to_drop_2],inplace=True)
            else:
                if len(indexes_to_drop)!=0:
                    data_t.drop(data_t.index[indexes_to_drop],inplace=True)
        '''CLUSTERING CHANGES END'''
        utils.logger(logger,correlationId,'INFO',('Clustering Completed at : '+str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        
        utils.updQdb(correlationId,'P','70',pageInfo,userId)
        #X_data, Y_data 
        if target_variable in LEcols:             
            Atarget_variable = target_variable + '_' + 'L' 
        else:
            Atarget_variable = target_variable

        message_uniquetarget=[]
        if problemtypef =='Classification' or problemtypef=='Multi_Class':
            if data_t[Atarget_variable].nunique()>2:
                problemtypef ='Multi_Class'
                message_uniquetarget.append('null')
            elif data_t[Atarget_variable].nunique()==2:
                problemtypef ='Classification'
                message_uniquetarget.append('null')
            else:
                problemtypef ='Error,less than 2 classes!'
                message_uniquetarget.append('Error,less than 2 classes!')
        #x_cols = [col for col in data_t.columns if col not in [Atarget_variable,'All_Text']]
        x_data = data_t.loc[:,data_t.columns != Atarget_variable]
        '''CLUSTERING CHANGES START'''
        if problemtypeflag:
            x_data = data_t.loc[:,list(set(data_t.columns) - set(text_list) -set(['All_Text'])-set([Atarget_variable]))]
            data_cols = pd.Index(list(set(data_cols.append(pd.Index(cluster_columns)))-set(text_cols_drop) - set(text_list) -set(['All_Text']) -set([Atarget_variable])-set(date_cols)-set(temp_col)))
        else:
            data_cols = pd.Index(list(set(data_cols)-set([Atarget_variable])-set(date_cols)-set(temp_col)))
        '''CLUSTERING CHANGES END'''
        #print(data_cols)
        x_data_col = x_data.columns
        y_data = data_t[Atarget_variable]  
        '''CHANGES START HERE'''
        '''                         
        if features_created!=[]: 
            data_cols = data_cols.append(pd.Index(features_created))
        '''                      
        '''CHANGES END HERE'''
        x_datadtypes = dict(x_data.dtypes)
        utils.logger(logger,correlationId,'INFO',str(x_datadtypes),str(None)) 
        
                                   
        
        if problemtypef=='Error,less than 2 classes!':
            raise Exception("Model has only one class in the Target Variable. Please change the filters/data/targetvariable and retry again")
            #utils.updQdb(correlationId,'E',"Model has only one class in the Target Variable. Please change the filters/data/targetvariable and retry again",pageInfo,userId)
            '''CHANGES START HERE'''
            #return
        # Feature Importance
            
        elif (problemtypef,x_data,y_data,data_cols) is not None: # and x_data is not None and y_data is not None and data_cols is not             
            feat_imp = {}
            uniqueIdentifier_dropped_flag = False
            if uniqueIdentifier in x_data.columns.tolist():
                x_data.drop([uniqueIdentifier], axis=1,inplace=True)
                uniqueIdentifier_dropped_flag = True
            if x_data.shape[1]!=0:
                from datapreprocessing import Feature_Importance
                Feat_Imp = Feature_Importance.feature_importance(problemtypef,x_data,y_data,data_cols)
                '''CHANGES END HERE'''
                for i,j in Feat_Imp.iterrows():  
                    temp = {}
                    temp.update({'Selection':'True'})
                    temp.update({'Value':j[1]})
                    feat_imp.update({j[0]:temp})
                if uniqueIdentifier and not timeSeries:
                    temp = {}
                    temp.update({'Selection':'True'})
                    temp.update({'Value':0})
                    feat_imp.update({uniqueIdentifier:temp})
            else:
                if uniqueIdentifier_dropped_flag:
                    temp = {}
                    temp.update({'Selection':'True'})
                    temp.update({'Value':0})
                    feat_imp.update({uniqueIdentifier:temp})
            '''CLUSTERING CHANGES START'''
            if not clustering_flag:
                temp = {}
                temp.update({'Selection':'True'})
                temp.update({'Value':0})
                feat_imp.update({'All_Text':temp})   
            '''CLUSTERING CHANGES END'''
        else:
            raise Exception("Insufficient parameters to find Feature Importance")
            #utils.updQdb(correlationId,'E',"Insufficient parameters to find Feature Importance",pageInfo,userId)          
            #return
        utils.updQdb(correlationId,'P','80',pageInfo,userId)
        if problemtypef=='Multi_Class':
             dbproconn,dbprocollection = utils.open_dbconn("DE_DataProcessing")
             data_json = dbprocollection.find({"CorrelationId" :correlationId})  
             try: 
                user_consent=data_json[0].get('SmoteMulticlass').get('UserConsent')
             except AttributeError as e:
                user_consent = False
             if user_consent=="True":
                 from datapreprocessing import smote
                 smote_F=smote.check_SMOTE_multiclass(y_data,ratio=None,user_consent=True)
             else:
                 smote_F=False
        
        #aggregation for TimeSeries
        if problemtypef == 'TimeSeries':
            from datapreprocessing import aggregation
            if uniqueIdentifier and uniqueIdentifier in data_t.columns:
                data_t.drop(uniqueIdentifier,axis=1,inplace=True)
            data_t = aggregation.main(correlationId,data_t,input_columns[0],Atarget_variable,interapolationTechnique)
            data_cols_t = [data_t[list(data_t.keys())[0]].index.name]+list(data_t[list(data_t.keys())[0]].columns)
                
        # saving PreProcessed Data 
        #print ("herere",data_t.shape)
        if smote_F == 'True':
             # SMOTE        
            try:
                from datapreprocessing import smote
                x_data,y_data = smote.smote(x_data,y_data)
                if uniqueIdentifier and uniqueIdentifier in x_data_col:
                   list(x_data_col).remove(uniqueIdentifier) 
                smote_data = pd.DataFrame(data=x_data, columns=x_data_col)
                del x_data
                smote_data[Atarget_variable] = y_data 
                datappcols = smote_data.shape[1] 
                datappsize = (sys.getsizeof(smote_data)/ 1024) / 1024               
                filesize = utils.save_data(correlationId,pageInfo,userId,'DE_PreProcessedData',data=smote_data,datapre=True)
            except:
                datappcols = data_t.shape[0] 
                datappsize = (sys.getsizeof(data_t)/ 1024) / 1024               
                filesize = utils.save_data(correlationId,pageInfo,userId,'DE_PreProcessedData',data=data_t,datapre=True)                
        else:
            if not timeSeries:
                datappcols = data_t.shape[0] 
                datappsize = (sys.getsizeof(data_t)/ 1024) / 1024               
                filesize = utils.save_data(correlationId,pageInfo,userId,'DE_PreProcessedData',data=data_t,datapre=True)
            else:
                datappcols = int(sum([data_t[each].shape[0] for each in data_t])/len(data_t)) 
                datappsize = (sys.getsizeof(data_t)/ 1024) / 1024               
                filesize = utils.save_data_timeseries(correlationId,pageInfo,userId,'DE_PreProcessedData',data_cols_t,data=data_t,datapre=True)
            
        utils.updQdb(correlationId,'P','85',pageInfo,userId)
        
        if not timeSeries: 
            if problemtypef=='Multi_Class' or problemtypef=='Classification' :
                if smote_F=='True':
                    y_data=pd.Series(y_data)
                #if y_data.value_counts().iloc[-1]<2:
                    #print('No')
                   #raise Exception("Target variable has only one instance of each value in the Target Variable. No of K-folds is less than 2. Aleast 2 records needed per class to proceed with training")
                   #utils.updQdb(correlationId,'E',"Target variable has only one instance of each value in the Target Variable. No of K-folds is less than 2. Aleast 2 records needed per class to proceed with training",pageInfo,userId)
                   #return
                if len(data_t.index)<min_df:
                    #print('Less than 20')
                    raise Exception("No of Records for MultiClass/Classification is less than {}".format(min_df))
                    #utils.updQdb(correlationId,'E',"No of Records for MultiClass/Classification is less than str(min_df)",pageInfo,userId)
                    #return
                else:
                    if y_data.value_counts().iloc[-1]<10:
                        Kfold=int(y_data.value_counts().iloc[-1])
                    elif y_data.value_counts().iloc[-1]>=10:
                        Kfold=10
                    
                   
            else:
                if problemtypef=='Regression':
                    if smote_F=='True':
                        data_t=smote_data
                    if len(data_t.index)<20:
                        Kfold=2
                    elif len(data_t.index)>=20:
                        if len(data_t.index)/10>=10:
                            Kfold=10
                        else:
                            Kfold = int(len(data_t.index)/10)
                    elif len(data_t.index)/10<10:
                        Kfold=int(len(data_t.index)/10)
        else:
            if len(data_t[list(data_t.keys())[0]].index)<2:
                raise Exception("No of Records for TimeSeries is less than {}".format(2))
            Kfold=10        
        
        del y_data
        #print("Encoding Done3")
        #if ngram had to reduced, upate the same in de_datapreprocessing
    
        if problemtypeflag and not clustering_flag:
            dbproconn,dbprocollection = utils.open_dbconn("DE_DataProcessing")
            data_json = list(dbprocollection.find({"CorrelationId" :correlationId}))
            if EnDeRequired :
                t = base64.b64decode(data_json[0].get('DataModification'))
                data_json[0]['DataModification']  =  eval(EncryptData.DescryptIt(t))
            text_dict= data_json[0].get('DataModification',{}).get('TextDataPreprocessing')
            #print ("balh",text_dict,"ngram",n_gram)
            if n_gram != tuple(text_dict.get('N-grams')):
                data_json[0]['DataModification']['TextDataPreprocessing']['N-grams'] = list(n_gram)
                if EnDeRequired :
                     t = data_json[0].get('DataModification')
                     data_json[0]['DataModification']  =  EncryptData.EncryptIt(json.dumps(t))  
                changeJson = data_json[0]
                dbprocollection.update_many({"CorrelationId"     : correlationId},
                                          { "$set":changeJson},upsert=True)
            dbconn.close()
        # Save Feature Importance
        dbconn,dbcollection = utils.open_dbconn('ME_FeatureSelection')
        import uuid
        Id = str(uuid.uuid4())
        import pickle
        dbcollection.update_many({"CorrelationId"     : correlationId},
                            { "$set":{      
                               "CorrelationId"     : correlationId,
                               "FeatureImportance" : feat_imp,
                               "Split_Column"      : {"TargetColumn":target_variable},
                               "Actual_Target"     : Atarget_variable,
                               "Train_Test_Split"  : {"TrainingData": 70},
                               "KFoldValidation"   : {"ApplyKFold": Kfold},
                               "StratifiedSampling": "True",                               
                               "pageInfo"          : pageInfo,
                               "CreatedBy"         : userId,
                               "UniqueTarget"      : data_unique_vals,
                               "UniqueTarget_Message": message_uniquetarget,
                               #'''CHANGES START HERE'''
                               #"Feature_Not_Created" : feature_not_created,
                               #"Features_Created" : features_created,
                               #"Map_Encode_New_Feature":list(map_encode_new_feature.keys()),
                               #"Add_Feature_Value_Store":value_store,                               
                               #'''CHANGES END HERE'''
                               #'''CLUSTERING CHANGES START'''
                               "Cluster_Columns" : cluster_columns,
                               "NLP_Flag":problemtypeflag,
                               "Clustering_Flag": clustering_flag,
                               "silhouette_graph": silhouette_graph,
                               #'''CLUSTERING CHANGES END'''
                               "Text_Null_Columns_Less20":drop_text_cols,
                               "Frequency_dict":freq_dict,
                               "Final_Text_Columns": text_list,
                               #'''AUTOBINNING CHANGES START HERE'''
                               "Binning_Dict":str(pickle.dumps(bins_dict))
                               #'''AUTOBINNING CHANGES END HERE'''
                               }},upsert=True)
        dbconn.close()
        utils.updQdb(correlationId,'P','90',pageInfo,userId)
        #print("Encoding Done4")
        #Estimate model runtime
        dbconn,dbcollection = utils.open_dbconn('MLDL_ModelsMaster')
        models_data = dbcollection.find({"_id" :'29529e26-b07c-4138-838b-450a9b126efd'}) 
        dbconn.close()        
        #if problemtypef == 'Text_Classification':
        #    allModels = models_data[0].get('Models',{}).get('Classification')
        #else:
        allModels = models_data[0].get('Models',{}).get(problemtypef)
        instaregression = utils.check_instaregression(correlationId)
        instaml = utils.check_instaml(correlationId)
        if instaregression or instaml:
            models = [k for k,v in allModels.items() if v == 'True']
        else:
            models = [key for key,value in allModels.items()]
        model_j = models_data[0].get('ModelsERT')
        lencmdb_j = model_j.get('LEncM')
        lencmdb = pickle.loads(lencmdb_j)
        modeldb_j = model_j.get('Model')
        modeldb = None#pickle.loads(modeldb_j)
        del modeldb_j
        temp_modf = {}
        for modelindx in range(len(models)):             
            temp_mod={} 
            if problemtypef != 'TimeSeries':
                if models[modelindx] != "Generalized Linear Model":
                    #ERT=Models_EstimatedRunTime.main(ndata_cols,datasize,datappcols,datappsize,models[modelindx],lencmdb,modeldb)
                    ERT=[100]
                    temp_mod.update({"Train_model":allModels[models[modelindx]],
                         "EstimatedRunTime": round(ERT[0], 2)})
                else:
                    temp_mod.update({"Train_model":allModels[models[modelindx]],
                             "EstimatedRunTime": .52})
            else:
                if models[modelindx] == "SARIMA":
                    temp_mod.update({"Train_model":allModels[models[modelindx]],
                             "EstimatedRunTime": 950*len(data_t.keys())})
                else:
                    temp_mod.update({"Train_model":allModels[models[modelindx]],
                             "EstimatedRunTime": 15*len(data_t.keys())})
            temp_modf.update({models[modelindx]:temp_mod})
#        Insert_ME_RecommendedModels(correlationId,problemtypef,pageInfo,userId,temp_modf)
        #print("Encoding Done5")
        dbconn,dbcollection = utils.open_dbconn('ME_RecommendedModels')
        import uuid
        Id = str(uuid.uuid4())
        dbcollection.update_many({"CorrelationId":correlationId},
            { "$set":{

            "CorrelationId": correlationId,
            "ProblemType"  : problemtypef,
            "SelectedModels": temp_modf,
            "pageInfo"      : pageInfo,
            "retrain":True,
            "CreatedOn": str(datetime.datetime.now())      ,
            "CreatedByUser": userId,
            "ModifiedOn": str(datetime.datetime.now()) ,
            "ModifiedByUser": userId}},upsert = True)    
        dbconn.close()
        utils.updQdb(correlationId,'C','100',pageInfo,userId)  
        if flag == 'AutoTrain':
            dbconn, dbcollection = utils.open_dbconn("DE_DataProcessing")
            dbcollection.update_one({"CorrelationId": correlationId},{"$set":{"DataTransformationApplied": True}})
            dbconn.close()
            dbconn, dbcollection = utils.open_dbconn("PS_BusinessProblem")
            data_json = list(dbcollection.find({"CorrelationId": correlationId}))
            dbconn.close()
            if "TemplateUsecaseID" in data_json[0]:
                templateusecaseid = data_json[0]['TemplateUsecaseID']
                try:
                    AllData_Flag = checkFlagintemplate(templateusecaseid)
                except:
                    AllData_Flag = False				
                update_AllData_Flag(correlationId,AllData_Flag)
                if templateusecaseid != None:
                    problemtype = gettemplateproblemtype(templateusecaseid)
                else:
                    problemtype = gettemplateproblemtype(correlationId)
                key_list = UpdateRecommendedModels(correlationId,problemtype)
                dbconn, dbcollection = utils.open_dbconn("SSAI_IngrainRequests")
                data_preprocessing = list(dbcollection.find({"CorrelationId": correlationId, "pageInfo": "DataPreprocessing"}))
                for model in key_list:
                    id = str(uuid.uuid4())
                    dbcollection.insert({
                        "_id": str(uuid.uuid4()),
                        "CorrelationId":correlationId,
                        "RequestId":id,
                        "ProcessId":data_preprocessing[0].get('ProcessId'),
                        "Status":"P",
                        "ModelName":model,
                        "RequestStatus":"In - Progress",
                        "RetryCount":0,
                        "ProblemType":utils.fetchProblemType(correlationId)[0],
                        "Message":"null",
                        "UniId":data_preprocessing[0].get('UniId'),
                        "TemplateUseCaseID":templateusecaseid,
                        "Progress":"In - Progress",
                        "pageInfo":"RecommendedAI",
                        "ParamArgs":"{}",
                        "Function":"RecommendedAI",
                        "CreatedByUser":data_preprocessing[0].get('CreatedByUser'),
                        "CreatedOn":str(datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                        "ModifiedByUser":data_preprocessing[0].get('ModifiedByUser'),
                        "ModifiedOn":str(datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                        "LastProcessedOn":"null",
                        "AppID":data_preprocessing[0].get('AppID'),
                        "ClientId":data_preprocessing[0].get('ClientId'),
                        "DeliveryconstructId":data_preprocessing[0].get('DeliveryconstructId')
                        })
                sys.argv[2] = id
                sys.argv[3] = "RecommendedAI"
                sys.argv[5] = '0'
                try:
                    utils.updateautotrain_record(correlationId,"ME","70")	
                except:
                    utils.logger(logger,correlationId,'INFO',("Auto train not needed"),str(None))
                sys.argv.append('True')
						
                from main import invokeRecommendedAI
                dbconn.close()


        end=time.time() 
        #local_vars = list(locals().items())
        #for var, obj in local_vars:
        #    print(var, sys.getsizeof(obj))

    
    except Exception as e:
#        dbproconn.close()
        utils.updQdb(correlationId,'E',str(e.args),pageInfo,userId)
        if flag == 'AutoTrain':
            utils.updateautotrain_record_forerror(correlationId,"ME")
        utils.logger(logger,correlationId,'ERROR','Trace',str(None))
        utils.save_Py_Logs(logger,correlationId)
    #else:
        #utils.logger(logger,correlationId,'INFO',('Data Preprocessing: '+" Process completed successfully at "+str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        #utils.save_Py_Logs(logger,correlationId)
        

    
##correlationId='e73d356b-3252-45f8-9b95-1310bca26774'
##pageInfo='DataPreprocessing'
##userId=1
##main(correlationId,pageInfo,userId)