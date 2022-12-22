import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
from SSAIutils import utils
from datapreprocessing import DataEncoding
from datapreprocessing import Data_Modification
import pandas as pd 
import lime  
import lime.lime_tabular
import datetime
import uuid
import time
import re 
import collections
import numpy as np
import math
import json
#from datapreprocessing import Textclassification
#from sklearn.feature_extraction.text import CountVectorizer
#from sklearn.feature_extraction.text import TfidfVectorizer
#from scipy.sparse import hstack  
from sklearn.preprocessing import LabelEncoder,StandardScaler
#from scipy.sparse import csr_matrix
from SSAIutils import EncryptData #En............................
import base64
import json
def transform_and_predict(tf_data,input_columns,exc_cols,input_data_types,target_variable,correlationId,WFId,requestId,pageinfo,final):    
    ##########
    EnDeRequired = utils.getEncryptionFlag(correlationId)
    exc_cols=[]
    dbproconn,dbprocollection = utils.open_dbconn("WhatIfAnalysis")
    data_json = dbprocollection.find({"CorrelationId" :correlationId,"WFId": WFId})  
    dbproconn.close()
    time.sleep(1)
    Features = data_json[0].get('Features')
    if EnDeRequired:     
        t = base64.b64decode(Features)
        Features = eval(EncryptData.DescryptIt(t))            
    dataF = {}
    for key, value in Features.items():
        if value.get('Selection') == 'False':
            exc_cols.append(value.get('Name'))
        dataF.update({value.get('Name'):value.get('Value')})                        
    
    data = pd.DataFrame([dataF]) 
    data = utils.DateTimeStampParser(data)                                  
    data_cols = list(data.columns)
    data_cols.sort()
    ######
    data=tf_data.copy()
    dbproconn,dbprocollection = utils.open_dbconn("SSAI_IngrainRequests")
    data_json_model = dbprocollection.find({"CorrelationId" :correlationId,"RequestId":requestId,"pageInfo":pageinfo}) 
    dbproconn.close()
    model_info = list(data_json_model)
    model = model_info[0]["ModelName"]
    
    dbconn,dbcollection = utils.open_dbconn("SSAI-savedModels")
    data_savedMod = dbcollection.find({"CorrelationId" :correlationId}) 
    count = dbcollection.count_documents({"CorrelationId" :correlationId}) 
    trans = {}
    for indx in range(count):
        if data_savedMod[indx].get('FileType') == ('StandardScaler' or 'Normalizer'):            
            trans.update({data_savedMod[indx].get('FileName') :data_savedMod[indx].get('FilePath') })            
    dbconn.close()
    time.sleep(1)
    dbproconn,dbprocollection = utils.open_dbconn("DE_DataProcessing")
    data_json = list(dbprocollection.find({"CorrelationId" :correlationId}))   #DE.....................................
    if EnDeRequired :
         t = base64.b64decode(data_json[0].get('DataModification'))
         data_json[0]['DataModification']  =  eval(EncryptData.DescryptIt(t))
#    dbproconn,dbprocollection = utils.open_dbconn("DE_DataProcessing")
#    data_json = dbprocollection.find({"CorrelationId" :correlationId}) 
#    dbproconn.close()
#    time.sleep(1)
#    
    #Scalers 
    Features = data_json[0].get('DataModification',{}).get('Features')  ##########################################################
    if "Interpolation"in Features:
        del Features["Interpolation"]
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
     
    if data.isnull().values.any():
    #        data = data.fillna(value=0) 
        data=data.dropna(axis=0)
    
    binning_data= data_json[0].get('DataModification',{}).get('ColumnBinning')
    if binning_data:
        if target_variable in binning_data.keys():
            del binning_data[target_variable]
        temp = {}
        for  keys,values in binning_data.items():                                
            if values.get('ChangeRequest',{}).get('ChangeRequest') == 'True' and values.get('PChangeRequest',{}).get('PChangeRequest') == 'True':                                        
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
    
    
    '''CHANGES START HERE'''
    #print("adding new features") 
    map_encode_new_feature={}
    features_created = [] 
    
    dbconn,dbcollection = utils.open_dbconn('DE_AddNewFeature')
    data_features = dbcollection.find({"CorrelationId" :correlationId}) 
    try: 
        features_created          = data_features[0].get("Features_Created")
        map_encode_new_feature = data_features[0].get("Map_Encode_New_Feature")
    except Exception as e:
        error_encounterd = str(e.args[0])
    
    if len(features_created)>0:
        if len(map_encode_new_feature)>0:
            for item in map_encode_new_feature:
                map_encode_new_feature[item] = {'attribute': 'Nominal','encoding': 'Label Encoding','ChangeRequest': 'True','PChangeRequest': 'False'}
        
    Data_to_Encode=data_json[0].get('DataEncoding')  
    '''CHANGES START HERE'''
    if len(map_encode_new_feature)>0:
        Data_to_Encode.update(map_encode_new_feature)
    '''CHANGES END HERE'''
    OHEcols = []
    LEcols = []
    encoders = {}
#    encoders1={}

    for keys,values in Data_to_Encode.items():                
        if values.get('encoding') == 'One Hot Encoding' and (values.get('ChangeRequest') == 'True' and values.get('PChangeRequest') != 'True') and (keys != 'ChangeRequest' and keys!='PChangeRequest'):
            OHEcols.append(keys)
        elif values.get('encoding') == 'Label Encoding' and (values.get('ChangeRequest') == 'True' and values.get('PChangeRequest') != 'True') and (keys != 'ChangeRequest' and keys!='PChangeRequest'):
            LEcols.append(keys)
    
    OHEcols=[value for value in OHEcols if value in input_columns]                       
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
    
    LEcols=[value for value in LEcols if value in input_columns]
    
    if len(LEcols)>0:            
        #data,data_le = DataEncoding.Label_Encode_Dec(lencm,data,LEcols)
        data,data_le,classMappingUpdated = DataEncoding.Label_Encode_Dec_modified(lencm,data,LEcols)
        encoders.update({'LE':{lencm:Lenc_cols}})
        if len(classMapping)!=len(classMappingUpdated):
            for item in classMappingUpdated.keys():
                #print(item)
                if item not in classMapping.keys():
                    #print(item)
                    lencm.classes_=np.append(lencm.classes_,item)
    else:
        pass
        
    
    if data.isnull().values.any():
        data = data.fillna(value=0) 
    
    LEcols=[value for value in LEcols if value in input_columns ]
    drop_cols = OHEcols + LEcols
    
    if target_variable in list(data.columns):
        data = data.drop(target_variable,axis=1)
    
    if drop_cols:
        data_t = data.drop(drop_cols,axis=1)
    else:
        data_t = data  
    
    #if data_t.isnull().values.any():
    #    utils.updQdb(correlationId,'E',"Their are nulls in data, preprocess data, Missing values",pageInfo,userId,UniId = WFId)            
    #    utils.save_Py_Logs(logger,correlationId)
    #    return    
            
    temp_col1 = list(data_t.columns[data_t.dtypes == 'datetime64[ns]'])
    
    temp_col2 = list(data_t.columns[(data_t.dtypes == 'object')])
    a=[]
    for i in temp_col2:
        if input_data_types[i] in ["int64","float64"]:
            a.append(i)
            
    data_types={}
    for i in temp_col2:
        data_types[i] = input_data_types[i]
    data_t =data_t.astype(data_types)
    
    temp_col2 = list(set(temp_col2)-set(a))
    temp_col=temp_col1+temp_col2
    
    if len(temp_col) != 0:  
        data_t = data_t.drop(temp_col,axis=1)
           
    
    #if ('object' or 'datetime64[ns]') in list(data_t.dtypes):
    #    data_tdtypes = dict(data_t.dtypes)
    #    utils.logger(logger,correlationId,'INFO', ('data dtypes'+str(data_tdtypes)))
    #    utils.updQdb(correlationId,'E',"Their are strings/datatime stamps in data, preprocess data, Data Encoding",pageInfo,userId,UniId = WFId)                
    #    utils.save_Py_Logs(logger,correlationId)
    #    return   
    
    MLDL_Model,_,ProblemType,traincols = utils.get_pickle_file(correlationId,model,'MLDL_Model')  
    
    #######
    bulk = False
    #######
    if ProblemType == "classification" or ProblemType== "Multi_Class":
        MLDL_Model =  MLDL_Model.best_estimator_
        if model=='XGBoost Classifier':
            traincols=MLDL_Model.get_booster().feature_names
        if bulk =='False':
            for coll in exc_cols:            
                for colm in list(data_t.columns):
                    if re.search(str(coll+'_'),colm):
                        data_t[colm] = 0
            
        test_data = data_t[traincols]
    elif ProblemType == "regression":
        test_data = data_t[traincols]
    testdat=test_data.copy()
    pred_series=MLDL_Model.predict(testdat)
    if (ProblemType !="regression"):
        tf_data[target_variable]=lencm.inverse_transform(pred_series)
    else:
        tf_data[target_variable]=(pred_series)
    #        test_data_cols = list(test_data.columns)
    final_data = tf_data.copy()
    
#########################################################################
    predictions={}
    if(final==True):
        tf_data.drop(labels=target_variable,axis=1,inplace=True)
        tf_data_cols =list(tf_data.columns)
        tf_data_vals = list(tf_data.values[0])
        tf_data_dict = dict(zip(tf_data_cols,tf_data_vals))
#        tf_data_json = json.dumps(tf_data_dict)
        if ProblemType == "classification" or ProblemType== "Multi_Class":
            predict_fn = lambda x: MLDL_Model.predict_proba(pd.DataFrame(x,columns=traincols)).astype(float)
        else:
            predict_fn = lambda x: MLDL_Model.predict(x).astype(float)           
        exp_data = utils.data_from_chunks(correlationId,'DE_PreProcessedData',lime=True)
        exp_data = exp_data[traincols]
        explainer = lime.lime_tabular.LimeTabularExplainer(exp_data.values,mode = 'classification' if ProblemType=='Multi_Class' else ProblemType,feature_names = traincols)
        
        time.sleep(1)
        
        if ProblemType == "classification" or ProblemType== "Multi_Class":
            dbconn,dbcollection = utils.open_dbconn('ME_FeatureSelection')
            filterdata_json = dbcollection.find({"CorrelationId" :correlationId})    
            dbconn.close()
            time.sleep(1)
            target_classes = filterdata_json[0].get('UniqueTarget')
        
        predictions = {}    
        for indx,row in enumerate(test_data.iterrows()):  
            Atest_pred = 'None'
            temp_pred = {}
            temp_exp  = {}
            Data = dict(row[1])   
            
            testD = pd.DataFrame([Data],columns=traincols)
            test_pred = MLDL_Model.predict(testD)
            if LETarget == 'True':
                Atest_pred= lencm.inverse_transform(test_pred)
            else:
                Atest_pred = test_pred           
             
            try:                
                OGData = utils.get_OGData(row[1],encoders)
                Data = {}
                for ri,rj in OGData.iteritems():
                    if type(rj) == int:
                        Data.update({ri:int(rj)})
                    else:
                        Data.update({ri:rj})
            except:                
                Data = {}
                for ri,rj in row[1].iteritems():
                    if type(rj) == int:
                        Data.update({ri:int(rj)})
                    else:
                        Data.update({ri:rj})
            
            temp_pred.update({"Data": tf_data_dict})     
            temp_pred.update({"Prediction": Atest_pred[0]}) 
            
            if ProblemType == "classification":
                othpred=(set(target_classes) - set(Atest_pred)).pop()   #to display against probablitites         
                temp_pred.update({"OthPred": othpred}) 
            
                
                test_prob = MLDL_Model.predict_proba(testD)
                probablitites = {"Probab1":float(test_prob[0][0]),
                               "Probab2":float(test_prob[0][1])}
                temp_pred.update({"Probablities": probablitites})
            
            elif ProblemType== "Multi_Class":
                 common_classes=[x for x in lencm.classes_ if x in target_classes]
                 othpred=list(set(common_classes) - set(Atest_pred))
                 temp_pred.update({"OthPred": othpred}) 
                 
                 test_prob = MLDL_Model.predict_proba(testD)
                 dict1={}
                 for i in range(0,test_prob.shape[1]):
                     dict1[common_classes[i]]=float(test_prob[0][i])
                 
                 if len(dict1)>=5:
                     dict1=dict(sorted(dict1.items(),key=lambda t : t[1] , reverse=True)[:5])
                 else:
                     dict1=dict(sorted(dict1.items(),key=lambda t : t[1] , reverse=True))
                 
                 #dict1={k:round(v,2)*100 for k,v in dict1.items()}
                 temp_pred.update({"Probablities": dict1})
#            print (testD.values[0],predict_fn)    
            exp = explainer.explain_instance(testD.values[0],predict_fn, num_features=testD.shape[1])
            exp_map = exp.as_map()
            exp_feat = exp_map.get(1)
            for feat in exp_feat:            
                temp_exp.update({traincols[feat[0]] : feat[1]})  
           #temp_pred.update({"FeatureWeights":temp_exp})    
            temp_exp = collections.OrderedDict(sorted(temp_exp.items()))    
            FImp={}
            col_fimp = []
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
            temp_pred.update({"FeatureWeights":FImp_f})
                              
            predictions.update({'Prediction':temp_pred})
    
######################################################################################################

    return final_data,predictions
