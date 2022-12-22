# -*- coding: utf-8 -*-
"""
Created on Sun Feb 28 13:19:10 2021

@author: shrayani.mondal
"""

import platform
if platform.system() == 'Linux':
    conf_path = '/IngrAIn_Python/main/pythonconfig.ini'
    work_dir = '/IngrAIn_Python'
elif platform.system() == 'Windows':
    conf_path = '\IngrAIn_Python\main\pythonconfig.ini'
    work_dir = '\IngrAIn_Python'

import configparser,os
mainPath =os.getcwd()+work_dir
import warnings
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
sys.path.insert(0,mainPath)
import file_encryptor
config = configparser.RawConfigParser()
configpath = str(os.getcwd()) + str(conf_path)
try:
        config.read(configpath)
except UnicodeDecodeError:
        config = file_encryptor.get_configparser_obj(configpath)

warnings.filterwarnings("ignore", category=DeprecationWarning) 

import lime
import lime.lime_tabular
from datapreprocessing import DataEncoding
from datapreprocessing import Data_Modification
from datapreprocessing import Textclassification
from datapreprocessing import WoE_Binning
from SSAIutils import utils
import pandas as pd
from pandas import Timestamp
from numpy import nan
import numpy as np
from datapreprocessing import Add_Features
import time
import collections
import math
import re
from sklearn import preprocessing
import pickle
import spacy
import base64
import json
from SSAIutils import EncryptData
import fasttext,fasttext.util
if platform.system() == 'Linux':
    thai_model_path='fasttext_model/cc.th.100.bin'
elif platform.system() == 'Windows':
    thai_model_path='fasttext_model\\cc.th.100.bin'
  

def str_bool(s):
     if s=='True':
        return True
     if s=='False':
        return False
     else:
          raise ValueError

def Normalizer(data):
    scaler = preprocessing.Normalizer().fit(data)
    data = scaler.transform(data)
    return data

'''
uniqueId=  "af4fd74a-1770-40fe-ad3b-fc28df6dd437"
pageInfo="publishModel"	
correlationId=  "342b8b86-592d-4a40-b254-b46bcabf57d8"
'''

def main_wrapper(correlationId,uniqueId,pageInfo,mapped_column=None,unique_identifer=None,counter=None,cascaded_corid=None):
    dbconn,dbcollection = utils.open_dbconn('SSAI_PublishModel')
    data_json = list(dbcollection.find({"CorrelationId":cascaded_corid,"UniqueId": uniqueId}))

    dbconn.close()
    if len(data_json) == 1:
        predictions={}
        dbcollection.update_one({"CorrelationId":cascaded_corid,"UniqueId":uniqueId},{'$set':{         
                                'Status' : "P",
                                'Progress':"0",
                                'ErrorMessage':""
                               }}) 
        actual_data = str(data_json[0].get("ActualData"))
        k = 0
        if counter ==0:
            result=main(actual_data,k,correlationId,uniqueId,cascaded_corid,prediction = False,mapped_column=None,unique_identifer=None)
        elif counter > 0:
            result=main(actual_data,k,correlationId,uniqueId,cascaded_corid,prediction = False,mapped_column=mapped_column,unique_identifer=unique_identifer)
            
        predictions.update({"Chunk"+' '+str(k):result})
        
        
        

    else:
        predictions={}
        dbcollection.update_many({"CorrelationId":cascaded_corid,"UniqueId":uniqueId},{'$set':{         
                                'Status' : "P",
                                'Progress':"0",
                                'ErrorMessage':""}})
        if counter == 0:
            
            for i in range(len(data_json)):
                actual_data = str(data_json[i].get("ActualData"))
                k = data_json[i]["Chunk_number"]
                result=main(actual_data,k,correlationId,uniqueId,cascaded_corid,prediction = True,mapped_column=None,unique_identifer=None)
                predictions.update({"Chunk"+' '+str(k):result})
            
        elif counter > 0:
             for i in range(len(data_json)):
                actual_data = str(data_json[i].get("ActualData"))
                k = data_json[i]["Chunk_number"]
                result=main(actual_data,k,correlationId,uniqueId,cascaded_corid,prediction = True,mapped_column=mapped_column,unique_identifer=unique_identifer)
                predictions.update({"Chunk"+' '+str(k):result})
                
    return predictions   

def main(data,chunk_number,correlationId,uniqueId,cascaded_corid,prediction=None,mapped_column=None,unique_identifer=None):
    try:
        logger = utils.logger('Get',correlationId)
        EnDeRequired = utils.getEncryptionFlag(correlationId)
        unique_identifer=unique_identifer
        actual_data = data
        if cascaded_corid != "FMModel":
            CascadeEnDeRequired = utils.getEncryptionFlag(cascaded_corid)
        else:
            CascadeEnDeRequired = False
        
        if not isinstance(data, pd.DataFrame):
        
            if CascadeEnDeRequired:
                t = base64.b64decode(actual_data)
                try:
                    data = eval((EncryptData.DescryptIt(t)).encode('ascii',"ignore").decode('ascii'))
                except:
                    data = json.loads((EncryptData.DescryptIt(t)).encode('ascii',"ignore").decode('ascii'))
            else:
                data = actual_data.strip().rstrip()
                data = data.encode('ascii',"ignore").decode('ascii')
                if isinstance(data,str):
                    try:
                        data = eval(data)
                    except SyntaxError:
                        data = eval(''.join([i if ord(i) < 128 else ' ' for i in data]))
                    except:
                        data = json.loads(data)
            data = pd.DataFrame(data)
        
        if cascaded_corid != "FMModel":
            baseModelUniqueId = utils.getBaseModelUniqueId(cascaded_corid)
        else:
            baseModelUniqueId = None
        
        if unique_identifer!=None and isinstance(unique_identifer,dict):
            map_df = pd.DataFrame(mapped_column['Chunk '+str(chunk_number)], index=data[baseModelUniqueId])
            data.set_index(keys=data[baseModelUniqueId],inplace=True)
            concat_frame = pd.concat([map_df,data],axis=1)
            concat_frame.rename(columns = {baseModelUniqueId : unique_identifer["UniqueMapping"]["Target"]}, inplace=True)
            concat_frame.reset_index(inplace=True,drop=True)
            data = concat_frame.copy()
            del concat_frame
        elif unique_identifer==None:
            utils.logger(logger, correlationId, 'INFO', ('Unique Identifer is none'),str(uniqueId))
            
        else:
            raise Exception('Unique Identifer Mapping is incorrect, Kindly check once')
        try:
            for col in data.columns:
                try:
                    data[col].fillna(data[col].mode()[0], inplace=True) 
                except Exception:
                    utils.logger(logger, correlationId, 'INFO', ('Fill NA failed for columns'+str(col)),str(uniqueId))
        
        except:
            empty_cols = [col for col in data.columns if data[col].dropna().empty]
            if len(empty_cols)>0:
                missing_col_string = str(empty_cols)[1:-1]
                raise Exception("Values missing in " + missing_col_string + ". Please validate the data.")
            
            data = data.loc[:,~data.columns.duplicated()]
            for col in data.columns:
                data[col].fillna(data[col].mode()[0], inplace=True)
                
    except:
        raise Exception( "Facing issue in the data format passed. Please check the data format")

    
    
    dbconn,dbcollection = utils.open_dbconn('SSAI_DeployedModels')
    data_json = list(dbcollection.find({"CorrelationId":correlationId}))
    if data_json :
        selectedModel = data_json[0].get("ModelVersion") 
        ProblemType = data_json[0].get("ModelType")              
        dbconn,dbcollection = utils.open_dbconn("ME_FeatureSelection")
        data_json=list(dbcollection.find({"CorrelationId":correlationId},{'_id':0,"FeatureImportance":1}))        
        selectedFeatures = [each for each in data_json[0]["FeatureImportance"] if data_json[0]["FeatureImportance"][each]['Selection'] == "True"]
        
        dbconn,dbcollection = utils.open_dbconn("PS_BusinessProblem")
        data_json = list(dbcollection.find({"CorrelationId" :correlationId}))
        dbconn.close()
        target_variable = data_json[0].get('TargetColumn')
        try:
            UniqueIdentifier = data_json[0].get("TargetUniqueIdentifier")
            if not UniqueIdentifier or UniqueIdentifier == "":
                UniqueIdentifier = None
            else:
                selectedFeatures.remove(UniqueIdentifier)
        except:
            UniqueIdentifier = None
        if target_variable in list(data.columns):
            data.drop(target_variable,axis=1,inplace=True)
        if UniqueIdentifier and UniqueIdentifier in list(data.columns):
            UniqueIdentifierList = data[UniqueIdentifier].tolist()
            if UniqueIdentifier not in selectedFeatures: 
                data.drop(UniqueIdentifier, axis=1, inplace=True)
        else:
            UniqueIdentifierList = None
        if target_variable in selectedFeatures:
            selectedFeatures.remove(target_variable)
        dbconn,dbcollection = utils.open_dbconn('DE_AddNewFeature')
        data_json = dbcollection.find({"CorrelationId":correlationId})
        try:
            new_features = data_json[0].get("Features_Created")
            value_Store = data_json[0].get("Add_Feature_Value_Store")
            existingFeatures = data_json[0].get("Existing_Features")
            features_not_created = data_json[0].get("Feature_Not_Created")
        except Exception:
            new_features = []
            value_Store = []
            existingFeatures = []
            features_not_created =[]
        
        
        dbconn,dbcollection = utils.open_dbconn('ME_FeatureSelection')
        data_json = dbcollection.find({"CorrelationId":correlationId})
        
        '''AUTOBINNING CHANGES START HERE'''
        try:
            bins_dict = pickle.loads(eval(data_json[0].get("Binning_Dict")))
        except:
            bins_dict ={}
        '''AUTOBINNING CHANGES END HERE'''
        
        problemTypeflag=data_json[0].get("NLP_Flag")
        if isinstance(problemTypeflag,type(None)):
            problemTypeflag=False
        clustering_flag=data_json[0].get("Clustering_Flag")
        if isinstance(clustering_flag,type(None)):
            clustering_flag=True
        cluster_columns = []
        if problemTypeflag:
            if clustering_flag:
                columns_to_remove = data_json[0].get('Cluster_Columns') 
                cluster_columns = data_json[0].get('Cluster_Columns')
            elif not clustering_flag:
                columns_to_remove = ['All_Text']

            selectedFeatures = list(set(selectedFeatures)-set(columns_to_remove))
            dbproconn,dbprocollection = utils.open_dbconn("ME_FeatureSelection")
            data_json1 = dbprocollection.find({"CorrelationId" :correlationId})  
            final_text_columns=data_json1[0].get('Final_Text_Columns')
            selectedFeatures = selectedFeatures+final_text_columns
            og_text_columns = list(set(final_text_columns) - set(new_features))
            data_text_df=data[og_text_columns]
            
        data_copy = data.copy()
        
        if target_variable in list(data_copy.columns):
             data_copy.drop(target_variable,axis=1,inplace=True)
        if len(new_features)>0:
            selectedFeatures = list(set(existingFeatures).union(set(selectedFeatures)-set(new_features)))
        
        if not set(selectedFeatures).issubset(set(data.columns)):             
             if cascaded_corid != "FMModel":
                raise Exception( "Mismatch found in column names with the one trained on")
             else:
                missing_cols = list(set(selectedFeatures) - set(data.columns))
                missing_cols_dict = dict.fromkeys(missing_cols, 0)
                data = data.assign(**missing_cols_dict)
        if len(data.columns)/len(selectedFeatures) < 0.6 :        
             raise Exception( "Too many missing values")
        selectedFeatures = selectedFeatures + new_features
        if target_variable in selectedFeatures:
            selectedFeatures.remove(target_variable)
        
        data_cols = selectedFeatures
        #data should only contain selectedFeatures. Added by Shrayani
        data = data[selectedFeatures]
        
        dbconn,dbcollection = utils.open_dbconn("SSAI_DeployedModels")
        data_json1 = dbcollection.find({"CorrelationId" :correlationId})    
        dbconn.close()
        try:
            language = data_json1[0].get('Language').lower()
            #if language=='french':
            #    language  = 'english'
        except Exception:
            language = 'english'
            
        dbconn,dbcollection = utils.open_dbconn("DE_DataCleanup")
        features_data_json = dbcollection.find({"CorrelationId" :correlationId}) 
        dbconn.close()
        Features = features_data_json[0].get('Feature Name')
        if EnDeRequired :
            t = base64.b64decode(Features)
            Features = json.loads(EncryptData.DescryptIt(t))
        try:
            datacols_F=list(Features.keys())
            data_cols = data.columns        
            data_dtypes = dict(data.dtypes)
            ndata_cols = data.shape[1]
            datasize = (sys.getsizeof(data)/ 1024) / 1024 
        except:
            utils.logger(logger, correlationId, 'INFO', ('Finding datasize issue encountered'),str(uniqueId))
            
        
        features_dtypesf = {}
        for key,value in Features.items():        
            d_type = [key1 for key1,value1 in value.get('Datatype').items() if value1 =='True']
            if d_type:    
                features_dtypesf.update({key:d_type[0]})
            else:
                features_dtypesf.update({key:'ND'})
        cate = ['category']
        inte = ['float64', 'int64']
        try:
            for key4,value4 in features_dtypesf.items():   
                if key4 in data_dtypes.keys():           
                    if (data_dtypes.get(key4)).name in ['float64','int64']:
                        if value4 in cate:
                           data[key4] = data[key4].astype(str)                      
                    elif (data_dtypes.get(key4)).name == 'object':  
                        if value4 == inte[1]:   
                           data[key4] = data[key4].astype(int)
                        elif value4 == inte[0]:
                           data[key4] = data[key4].astype('float64')
                        pass
                    elif (data_dtypes.get(key4)).name =='bool':
                        data[key4] = data[key4].astype(str)
                else:
                    print("Not there")
        except Exception:
            raise Exception("Datatype selection not proper for column '{}'".format(key4))
        drop_cols_t=[]   
        text_list = []
        date_cols=[]
        for key5,value5 in features_dtypesf.items():
            
                if value5 in ['datetime64[ns]','datetime64[ns, UTC]']:
                   if key5 in selectedFeatures:
                        date_cols.append(key5)
        
        for cols_dates in date_cols:
            if cols_dates != UniqueIdentifier:
                data[cols_dates] = pd.to_datetime(data[cols_dates],dayfirst=True)                
        if len(new_features) >0:
            
            dbconn,dbcollection = utils.open_dbconn("DE_DataCleanup")
            features_data_json = dbcollection.find({"CorrelationId" :correlationId}) 
            dbconn.close()
            feature_mapping=features_data_json[0].get('NewAddFeatures')
            
                
            if feature_mapping != None and str(feature_mapping)!='' and len(feature_mapping) >=1:
                if len(features_not_created)>0:
                    for item in list(features_not_created.keys()):
                        feature_mapping.pop(item)
                data,feature_not_created = Add_Features.add_new_features(data,feature_mapping,pageInfo="publishModel",value_Store = value_Store )
                if len(feature_not_created) > 0:
                    raise Exception("Some derived feature was not created. {}".format(list(feature_not_created.keys())))
                else:
                    features_created = list(set(list(feature_mapping.keys())) - set(feature_not_created.keys()))
                    
                    for key in features_created:
                        if data.dtypes[key] not in ['int64','float64']:
                            if data.dtypes[key] =='bool':
                                data[key] = data[key].astype(str)
                    if problemTypeflag:
                        data_text_df = data[final_text_columns]
        dbconn,dbcollection = utils.open_dbconn("DE_DataCleanup")
        features_data_json = dbcollection.find({"CorrelationId" :correlationId}) 
        dbconn.close()

        Features = features_data_json[0].get('Feature Name')
        try:
            Features_new = features_data_json[0].get('Feature Name_New Feature')
        except Exception:
            utils.logger(logger, correlationId, 'INFO', ('Features_new doesnt exist'),str(uniqueId))
                    
        if EnDeRequired :
            t = base64.b64decode(Features)
            Features = json.loads(EncryptData.DescryptIt(t))
            try:
                t = base64.b64decode(Features_new)
                Features_new = json.loads(EncryptData.DescryptIt(t))
            except Exception:
                utils.logger(logger, correlationId, 'INFO', ('Decryption issue encountered'),str(uniqueId))
                
        try:                
            Features = {**Features, **Features_new}
        except Exception:
            utils.logger(logger, correlationId, 'INFO', ('Feature merging issue encountered'),str(uniqueId))
            
        features_dtypesf = {}
        
        for key,value in Features.items():        
            d_type = [key1 for key1,value1 in value.get('Datatype').items() if value1 =='True']
            if d_type:    
                features_dtypesf.update({key:d_type[0]})
            else:
                features_dtypesf.update({key:'ND'})
        drop_cols_t=[]   
        text_list = []
        date_cols=[]
        for key5,value5 in features_dtypesf.items():
            #if not timeSeries:            
                if value5 in ['Id','Text','datetime64[ns]','datetime64[ns, UTC]']:
                    drop_cols_t.append(key5)
                if value5 == 'Text':
                    text_list.append(key5)
                if value5 in ['datetime64[ns]','datetime64[ns, UTC]']:
                    date_cols.append(key5)
            #if timeSeries:
                if value5 in ['Id']:
                    drop_cols_t.append(key5)
            #if problemtypef=='Text_Classification':
            #    if value5 in ['Id','datetime64[ns]']:
            #        drop_cols_t.append(key5)
        if UniqueIdentifier in date_cols:
            date_cols = list(set(date_cols)-set([UniqueIdentifier]))
        for cols_dates in date_cols:
            #data[cols_dates] =  pd.to_datetime(data[cols_dates], format='YYYY-MM-DD HH:MM:SS')
            #data[cols_dates] = data[cols_dates].dt.strftime("YYYY-MM-DD HH:MM:SS")
            if cols_dates in data.columns:
                data[cols_dates] = pd.to_datetime(data[cols_dates],dayfirst=True)
            #data = data[data[cols_dates].notnull()]
            #data.reset_index(inplace = True, drop=True)
            
        #changes for datetime add features -end
        
      
        dbproconn,dbprocollection = utils.open_dbconn("DE_DataProcessing")
        data_json = list(dbprocollection.find({"CorrelationId" :correlationId}))   #DE.....................................
        if EnDeRequired :

             t = base64.b64decode(data_json[0].get('DataModification'))
             data_json[0]['DataModification']  =  eval(EncryptData.DescryptIt(t))
          
        dbconn,dbcollection = utils.open_dbconn("SSAI_savedModels")
        data_savedMod = dbcollection.find({"CorrelationId" :correlationId}) 
        count = dbcollection.count_documents({"CorrelationId" :correlationId}) 
        trans = {}
        for indx in range(count):
            if data_savedMod[indx].get('FileType') == ('StandardScaler' or 'Normalizer'):            
                trans.update({data_savedMod[indx].get('FileName') :data_savedMod[indx].get('FilePath') })            

        
        #Scalers 
        Features = data_json[0].get('DataModification',{}).get('Features')    
        for key,value in Features.items():
            data_transformations_data = value.get('Skewness')
            if data_transformations_data:
                value_t1 = [key2 for key2,value2 in data_transformations_data.items() if value2=='True' and (data_transformations_data.get('ChangeRequest') == 'True' and data_transformations_data.get('PChangeRequest') == 'True') and (key2 != 'ChangeRequest' and key2!='PChangeRequest')]            
                if value_t1:                
                    if value_t1[0] == 'Standardization':
                        wtd = 'StandardScaler'
                    elif value_t1[0] == 'Normalization':
                        wtd = 'Normalizer'                   
                    file_path = None   
                    file_path = trans.get(str(wtd)+'-'+str(key))                
                    if file_path !=None:
                        transm,_,_,_=utils.get_pickle_file(FilePath = file_path)                    
                        data= Data_Modification.scaler_dec(data,key,wtd,transm)                    
        data = data.dropna()
        if data.isnull().values.any():
    #        data = data.fillna(value=0) 
            #data=data.dropna(axis=0)
             raise Exception( "Null values present in data")
         
        
        #Binning      
        binning_data= data_json[0].get('DataModification',{}).get('ColumnBinning')
        temp = {}
        if binning_data:
            #if target_variable in binning_data.keys():
            #    del binning_data[target_variable]
            
            for  keys,values in binning_data.items():                                
                if values.get('ChangeRequest',{}).get('ChangeRequest') == 'True' and (values.get('PChangeRequest',{}).get('PChangeRequest') == 'True' or values.get('PChangeRequest',{}).get('PChangeRequest') == ''):                                        
                    for keys1,values1 in values.items():  
                        if values1.get('Binning') == 'True':
                            Ncat = values1.get('NewName') 
                            subcat =  values1.get('SubCatName')
                            temp.update({subcat:Ncat})
                    Ucat = list(set(temp.values()))      
                    for indx in range(len(Ucat)):
                        column = str(keys)                                       
                        subcat = [key2 for key2,value2 in temp.items() if value2 == Ucat[indx]]    
                        if column in data.columns:
                            data.loc[data[column].isin(subcat),column]=Ucat[indx]
        
        
        '''AUTOBINNING CHANGES START HERE'''
        if bins_dict!={}:
            data = WoE_Binning.bin_data(data, bins_dict)
        '''AUTOBINNING CHANGES END HERE'''
        
        text_dict= data_json[0].get('DataModification',{}).get('TextDataPreprocessing')
        if problemTypeflag:
            text_cols_drop=text_dict.get('TextColumnsDeletedByUser')
            
            
            dbconn,dbcollection = utils.open_dbconn('ME_FeatureSelection')
            data_features = dbcollection.find({"CorrelationId" :correlationId}) 
            features_created = data_features[0].get('Frequency_dict')
            text_cols = data_features[0].get('Final_Text_Columns')
            text_cols = list(set(text_cols)-set(text_cols_drop))
            data_text_df=data_text_df[text_cols]
            drop_text_cols = data_features[0].get('Text_Null_Columns_Less20')
            if text_cols_drop!=text_cols:
                if len(text_cols)!=0:
                        for col in text_cols:
                            data_text_df[col] = data_text_df[col].astype(str)
                for col in text_cols:
                    freq_most = features_created[col]['freq_most'] #remove most frequent words
                    freq_most = list(freq_most)
                    data_text_df[col]=data_text_df[col].apply(lambda x: " ".join(x for x in x.split() if x not in freq_most))
                    freq_rare = features_created[col]['freq_rare']  #remove least frequent words
                    freq_rare = list(freq_rare) #list of those words
                    data_text_df[col] = data_text_df[col].apply(lambda x: " ".join(x for x in x.split() if x not in freq_rare))
            
                for key5,value5 in text_dict.items():
                        if key5 not in ["Feature_Generator","N-grams","TextColumnsDeletedByUser","NumberOfCluster","Clustering","Aggregation"]:
                            for key6,value6 in value5.items():
                            #print('Key5',key6)
                            #print('value',value6)
                                if value6 in ['True','False']:
                                    text_dict[key5][key6] = str_bool(value6)
                                    
                
                
                
                for x in text_cols:
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
                            spacy_vectors = fasttext.load_model(thai_model_path)
        
                        data_text_df[x] = data_text_df[x].apply(Textclassification.text_process1,args=[True,text_dict[x]['Lemmitize'],text_dict[x]['Stemming'],text_dict[x]['Stopwords'],language,spacy_vectors])
                    
                #data_text_df.replace(r'^\s*$',np.nan,regex=True,inplace=True)
                
                
                #drop_text_cols=list(null_values_df_20)
                #final_text_cols=list(set(text_cols)-set(drop_text_cols))
                    
                if drop_text_cols!=[]:
                    data_text_df.drop(drop_text_cols,axis=1,inplace=True)
        
        #Encoding                    
        Data_to_Encode=data_json[0].get('DataEncoding')  
        '''CHANGES START HERE'''
        #if len(map_encode_new_feature)>0:
        #    Data_to_Encode.update(map_encode_new_feature)
        '''CHANGES END HERE'''
        OHEcols = []
        LEcols = []
        encoders = {}
        for keys,values in Data_to_Encode.items():                
            if values.get('encoding') == 'One Hot Encoding' and (values.get('ChangeRequest') == 'True' and values.get('PChangeRequest') != 'True') and (keys != 'ChangeRequest' and keys!='PChangeRequest'):
                OHEcols.append(keys)
            elif values.get('encoding') == 'Label Encoding' and (values.get('ChangeRequest') == 'True' and values.get('PChangeRequest') != 'True') and (keys != 'ChangeRequest' and keys!='PChangeRequest'):
                LEcols.append(keys)
        
        OHEcols=[value for value in OHEcols if value in selectedFeatures] 

        '''AUTOBINNING CHANGES START HERE'''
        #for the columns which  are autobinned, do not perform any kind of encoding
        if bins_dict!={}:
            autobinned_cols = [key for key in bins_dict]
            OHEcols = list(set(OHEcols) - set(autobinned_cols))
            LEcols = list(set(LEcols) - set(autobinned_cols))         
        '''AUTOBINNING CHANGES END HERE'''

                      
        if OHEcols:      
            ohem,_,enc_cols,_ = utils.get_pickle_file(correlationId,FileType='OHE')
            data,data_ohe = DataEncoding.one_hot_dec(ohem,data,OHEcols,enc_cols)
            encoders={'OHE':{ohem:{'EncCols':enc_cols,'OGCols':OHEcols}}}
        
        
        LETarget = 'None'
        if target_variable in LEcols:
            
            if len(LEcols)==1:          
                lencm,_,Lenc_cols,_ = utils.get_pickle_file(correlationId,FileType='LE')
                LEcols.remove(target_variable)  
                LETarget = 'True'
            else:
                lencm,_,Lenc_cols,_ = utils.get_pickle_file(correlationId,FileType='LE')
                LEcols.remove(target_variable)  
                Lenc_cols.remove(target_variable+'_L')
                LETarget = 'True'
                classMapping = dict(zip(lencm.classes_, lencm.transform(lencm.classes_).tolist()))
        elif LEcols:
            lencm,_,Lenc_cols,_ = utils.get_pickle_file(correlationId,FileType='LE')
            classMapping = dict(zip(lencm.classes_, lencm.transform(lencm.classes_).tolist()))
            
        
        
        LEcols=[value for value in LEcols if value in selectedFeatures]
        #if len(map_encode_new_feature)>0:
        #    LEcols = LEcols+list(map_encode_new_feature.keys())
        if len(LEcols)>0:            
            data,data_le = DataEncoding.Label_Encode_Dec(lencm,data,LEcols)
            encoders.update({'LE':{lencm:Lenc_cols}})
        else:
            pass
        #LEcols=[value for value in LEcols if value in selectedFeatures ]
        drop_cols = OHEcols + LEcols        
        
        if drop_cols:
            data_t = data.drop(drop_cols,axis=1)
        else:
            data_t = data  
               
        
        if data_t.isnull().values.any():            
            #utils.save_Py_Logs(logger,correlationId)
            raise Exception( "Their are nulls in data, preprocess data, Missing values")    
            
        temp_col = data_t.columns[data_t.dtypes == 'datetime64[ns]']
        date_cols2 = []
        for i in range(len(date_cols)):
            if date_cols[i] in data_t.columns:
                date_cols2.append(date_cols[i])
        temp_col =list(set( list(temp_col)+date_cols2))
        if len(temp_col) != 0:  
            data_t = data_t.drop(temp_col,axis=1)
        
       
       # if ('object' or 'datetime64[ns]') in list(data_t.dtypes):
        #    return "Their are strings/datatime stamps in data, preprocess data, Data Encoding"  
        #print (correlationId,selectedModel)
        MLDL_Model,_,ProblemType,traincols = utils.get_pickle_file(correlationId,selectedModel,'MLDL_Model')        
        if problemTypeflag and len(data_text_df.columns)!=0:
            data_t=pd.concat([data_t,data_text_df],axis=1)
            data_t.dropna(inplace=True)
            data_t['All_Text'] = data_t[list(set(data_t.columns) and set(data_text_df.columns))].astype(str).apply(' '.join, axis=1)
            data_t.drop(list(set(data_t.columns) and set(data_text_df.columns)),inplace=True,axis=1) 
            data_t.replace(r'^\s*$',np.nan,regex=True,inplace=True)
            data_t.dropna(inplace=True)
            data_copy.drop(index=list(set(data_copy.index)-set(data_t.index)),inplace=True)
            data_t.reset_index(drop=True, inplace=True)
            data_copy.reset_index(drop=True, inplace=True)
            
        if problemTypeflag:
            '''CLUSTERING CHNAGES START'''
            if clustering_flag:
                vec=text_dict.get("Feature_Generator")
                if vec=='Count Vectorizer':
                    vectorizer=utils.load_vectorizer(correlationId,'Count Vectorization')
                if vec=='Tfidf Vectorizer':
                    vectorizer=utils.load_vectorizer(correlationId,'Tfidf Vectorizer')
                if vec=='Word2Vec':
                    vectorizer=utils.load_vectorizer(correlationId,'Word2Vec')
                if vec=='Glove':
                    vectorizer=utils.load_vectorizer(correlationId,'Glove') 
                d= utils.load_cluster_dictionary(correlationId)
                optimal_clusters = len(cluster_columns)
                
                Cluster_Dict = {}
                for clust in range(optimal_clusters):
                    Cluster_Dict['Cluster'+str(clust)] = []
            
             
                text = data_t['All_Text']
                # Assigning count to clusters
                for row_text in text:
            #         print(text.index[text == row_text])
            #         print(row_text)
            #         Cluster_Dict['Row_Text'].append(row_text)
                    try:
                        vectorizer.fit_transform(pd.Series(row_text))
                    except ValueError:
                        for k in d.keys():
                            Cluster_Dict['Cluster'+str(k)].append(0)
                    except:
                        print('Error in "Ngram" selection')
                    else:
                        for key, values in d.items():
                            cluster_sum = 0
                            for val in vectorizer.get_feature_names():
                                if val in values:
                                    cluster_sum += 1
                            Cluster_Dict['Cluster'+str(key)].append(cluster_sum)
            
             
            
                # Creating Final Cluster Dataframe
                cluster_df = pd.DataFrame.from_dict(Cluster_Dict)
                data_t = pd.concat([data_t,cluster_df],axis=1)
                data_t.drop(["All_Text"],axis=1,inplace=True)
            elif not clustering_flag:
                if len(data_t['All_Text']) >=10 :
                    vectorized_df = Textclassification.clustering_optional_multithread(data_t['All_Text'],correlationId,"WFTeachTest",language)
                else:
                    vectorized_df = Textclassification.clustering_optional(data_t['All_Text'],correlationId,"WFTeachTest",language)
                data_t = data_t.join(vectorized_df)
                data_t = data_t.drop('All_Text',axis=1)
            
            
            
        '''TP CHANGES END'''
        #print("Orig cols::",data_t.columns)
        #print("Orig cols::",vectorized_df.columns)

        if ProblemType == "classification" or ProblemType== "Multi_Class":
            if selectedModel!='SVM Classifier':       
                MLDL_Model =  MLDL_Model.best_estimator_
            if selectedModel=='XGBoost Classifier':
                traincols=MLDL_Model.get_booster().feature_names
                if not clustering_flag:
                    traincols2 = [int(x) for x in traincols[-vectorized_df.shape[1]:]]
                    traincols = traincols[:-vectorized_df.shape[1]] + traincols2
            #print("traincols:: ",traincols)
            test_data = data_t[traincols]
        elif ProblemType == "regression":
            test_data = data_t[traincols]
#        test_data_cols = list(test_data.columns)
        #if ProblemType == "classification":
        if ProblemType == "classification" or ProblemType== "Multi_Class":
            predict_fn = lambda x: MLDL_Model.predict_proba(pd.DataFrame(x,columns=traincols)).astype(float)
        
        else:
            predict_fn = lambda x: MLDL_Model.predict(x).astype(float)           
        #if ProblemType!='Text_Classification':
        if clustering_flag:
            exp_data = utils.data_from_chunks(correlationId,'DE_PreProcessedData',lime=True)
            exp_data = exp_data[traincols]
            explainer = lime.lime_tabular.LimeTabularExplainer(exp_data.values,mode = 'classification' if ProblemType=='Multi_Class' else ProblemType,feature_names = traincols)
            
            time.sleep(1)
        
        if ProblemType == "classification" or ProblemType== "Multi_Class" :
            dbconn,dbcollection = utils.open_dbconn('ME_FeatureSelection')
            filterdata_json = dbcollection.find({"CorrelationId" :correlationId})    
            dbconn.close()
            time.sleep(1)
            target_classes = filterdata_json[0].get('UniqueTarget')
        #predictions = {} 
        if cascaded_corid != "FMModel":
            dbconn,dbcollection = utils.open_dbconn('SSAI_PublishModel')
            data_json = list(dbcollection.find({"UniqueId":uniqueId}))
        
        '''probab_output={'Probabilities':{'Yes':0.27,'No':0.73}
                       }'''
       
        predictions=[]
        
        for indx,row in enumerate(test_data.iterrows()):
            Atest_pred = 'None'
            temp_pred = {}
            temp_exp  = {}
            Data = dict(row[1])   
            
            testD = pd.DataFrame([Data],columns=traincols)
            if selectedModel =='SVM Regressor':
                testD= Normalizer(testD)
                testD=pd.DataFrame(testD,columns=traincols)
            test_pred = MLDL_Model.predict(testD)
            if UniqueIdentifierList:
                temp_pred.update({UniqueIdentifier: UniqueIdentifierList[indx]})
            temp_pred.update({"targetName": target_variable})
            if LETarget == 'True':
                Atest_pred= lencm.inverse_transform([int(test_pred)])
            else:
                Atest_pred = test_pred
            if ProblemType == "classification" or ProblemType== "Multi_Class" :				
                 temp_pred.update({"predictedValue": Atest_pred[0]}) 
            else:
                 temp_pred.update({"predictedValue": round(float(Atest_pred[0]),2)})			
            if ProblemType == "classification" or ProblemType=="Multi_Class":
                 common_classes=[x for x in lencm.classes_ if x in target_classes]

                 #fix for target variable binning - start
                 target_binnned_classes = set(target_classes).intersection(set(temp.keys()))
                 binned_target_list = []
                 if len(temp.keys())>0:
                     if len(target_binnned_classes)>0:
                         for item,value in temp.items():
                             if item in target_binnned_classes:
                                 binned_target_list.append(value)
                 binned_target_list = list(set(binned_target_list))
                 common_classes = common_classes+binned_target_list
                 #fix for target variable binning -end

                 othpred=list(set(common_classes) - set(Atest_pred))
                 temp_pred.update({"OthPred": othpred}) 
                 
                 #sorting common_classes in terms with predict_proba
                 common_classes1 =[]
                 for item in MLDL_Model.classes_:
                     
                     if lencm.inverse_transform([int(item)]) in common_classes:
                         common_classes1.append(lencm.inverse_transform([int(item)])[0])
                 if len(common_classes1) > 0:
                     common_classes = common_classes1 

                 test_prob = MLDL_Model.predict_proba(testD)
                 dict1={}
                 if selectedModel=='SVM Classifier' and len(common_classes1) > 2:
                     sortorder = np.argsort(MLDL_Model.decision_function(testD))[0][::-1]
                     test_prob[0].sort()	
                 for i in range(0,test_prob.shape[1]):
                     if selectedModel!='SVM Classifier' or len(common_classes1) < 3:
                        dict1[common_classes[i]]=float(test_prob[0][i])
                     else:
                        
                        dict1[common_classes[sortorder[i]]]=float(test_prob[0][::-1][i])
                 
                 dict1=dict(sorted(dict1.items(),key=lambda t : t[1] , reverse=True))                
                 dict1={k:round(v,4)*100 for k,v in dict1.items()}
                 temp_pred.update({"predictionProbability": dict1})
            if clustering_flag:
                exp = explainer.explain_instance(testD.values[0],predict_fn, num_features=testD.shape[1])
                exp_map = exp.as_map()
                exp_feat = exp_map.get(1)
                for feat in exp_feat:            
                    temp_exp.update({traincols[feat[0]] : feat[1]})  
                temp_exp = collections.OrderedDict(sorted(temp_exp.items()))    
                FImp={}
                col_fimp = []
                if problemTypeflag:
                    data_cols = list(data_cols)+list(cluster_df.columns)
                if len(new_features) >0:
                    data_cols = data_cols+new_features
                for col in data_cols:     
                    col_t = str(col)+'_'
                    for col1,imp in temp_exp.items():
                        if re.search(col_t,col1):
                            col_fimp.append(imp)
                        elif col == col1:
                            col_fimp.append(imp)
                         
                    FI = np.median(col_fimp) if not math.isnan(np.median(col_fimp)) else 0
                    FImp.update({col:FI}) 
                    col_fimp = []    
                FImp_f=collections.OrderedDict(sorted(FImp.items() , key=lambda t : t[1] , reverse=True))
                FImp_f = dict(FImp_f)
                temp_pred.update({"FeatureWeights":FImp_f})
            else:
                temp_pred.update({"FeatureWeights":'None'})
            
            if problemTypeflag:
                try:
                    word_cloud = Textclassification.wordcloud_weights(text[indx],FImp_f,d,vectorizer)
                    message = ""
                except Exception:
                    word_cloud =""
                    message = "The Word Cloud was not generated"
                temp_pred.update({"WordCloud":{"image":word_cloud,"message":message}})
            else:
                temp_pred.update({"WordCloud":{"image":"","message":""}})
            predictions.append(temp_pred)  
            if test_data.shape[0]>5  and cascaded_corid != 'FMModel':
                if  indx%5==0 :
                    if prediction == False:
                            dbcollection.update_one({"CorrelationId":correlationId,"UniqueId":uniqueId},{'$set':{         
                                'Progress' : str(int((indx+1)*100/test_data.shape[0]))                                                  
                               }}) 
                    else:
                            dbcollection.update_one({"CorrelationId":correlationId,"UniqueId":uniqueId,"Chunk_number":chunk_number},{'$set':{         
                                'Progress' : str(int((indx+1)*100/test_data.shape[0]))                                                  
                               }}) 
        '''
        if EnDeRequired :
            predictions = EncryptData.EncryptIt(json.dumps(predictions))
        else:
            predictions = str(predictions)
        '''
        if cascaded_corid == 'FMModel':
            return predictions
        return str(predictions)
        
    else:
        raise Exception ("Unable to find model for corresponding correlationId")
