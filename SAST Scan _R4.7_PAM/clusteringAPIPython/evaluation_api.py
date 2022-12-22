# -*- coding: utf-8 -*-
"""
Created on Mon Jun 22 04:47:04 2020

@author: saurav.b.mondal
"""

import time
from SSAIutils import utils
from datapreprocessing import DataEncoding
from datapreprocessing import Textclassification
import sys
import pandas as pd
import numpy as np
import datetime

import base64
import json
from main import EncryptData

def remove_ascii(t):
    if isinstance(t,str):
        return t.encode('ascii',"ignore").decode('ascii')
    else:
        return t
    

#dbconn, dbcollection = utils.open_dbconn("Clustering_BusinessProblem")
#data_json = list(dbcollection.find({"CorrelationId": correlationId}))
#dbconn.close()
#data=pd.DataFrame(data_json[0]['InputData'][1])

#data_test=[{"Suburb":"Caulfield North","Address":"7\\/19 Hawthorn Rd","Rooms":2,"Type":"u","Price":520500.0,"Method":"S","SellerG":"Gary","Date":"13\\/08\\/2016","Distance":8.1,"Postcode":3161.0,"Bedroom2":2.0,"Bathroom":1.0,"Car":1.0,"Landsize":0.0,"BuildingArea":"","YearBuilt":"","CouncilArea":"Glen Eira City Council","Lattitude":-37.8681,"Longtitude":145.0258,"Regionname":"Southern Metropolitan","Propertycount":6923.0}]

def main(correlationId,data_test,modelName,pageInfo,userId,Eval_Id,col_selectedbyuser,bulk=False,ClientID=None,DCUID=None,ServiceID=None,userdefinedmodelname=None,mapping=None, EnDeRequired=None):
    try:
        logger = utils.logger('Get',correlationId) 
        if EnDeRequired == None or not isinstance(EnDeRequired,bool):
            raise Exception("Encryption Flag is a mandatory field")
        message=""
        dbconn, dbcollection = utils.open_dbconn("Clustering_DataPreprocessing")
        data_json = list(dbcollection.find({"CorrelationId": correlationId}))
        dbconn.close()
        problem_type = 'Clustering'
        
        clustering_type=data_json[0]['Clustering_type']
        
        numerical_cols=data_json[0].get('Numerical_columns')
        category_cols=data_json[0].get('Category_columns')
        dropped_columns=data_json[0].get('Dropped_cols')
        text_cols=data_json[0].get('Text_columns')
        text_cols_drop=data_json[0].get('Dropped_text_cols')
        

        if EnDeRequired :
            t = base64.b64decode(data_test)
            data_test = json.loads(EncryptData.DescryptIt(t))
        
        data=pd.DataFrame(data_test)
        data_test=data.copy()
        
        for col in data_test.columns:
            data_test[col] = data_test[col].apply(remove_ascii)        
        
        #data_test.drop(dropped_columns,axis=1,inplace=True)
        
        if clustering_type.get('Text')=='True':
            data=data_test[text_cols]
        elif clustering_type.get('Non-Text')=='True':
            data=data_test[list(set(data_test.columns)- set(text_cols))]
        
            missing_values_nan=data.columns[data.isnull().any()].tolist() 
            for x in data.columns:  
                missing_values_empty=list((data.eq('')).all()[(data.eq('')).all()].index.values)
                data[missing_values_empty]=0
        
            for col in list(set(numerical_cols)&set(missing_values_nan)):
                data[col].fillna(0,inplace=True)
            for col in list(set(category_cols)&set(missing_values_nan)):
                data[col].fillna(0,inplace=True)
        
            for i in numerical_cols:        
                q1 = data[i].quantile(0.25)
                q3 = data[i].quantile(0.75)
                iqr = q3-q1 
                data[i] = np.where(((data[i] < (q1 - 1.5 * iqr)) |(data[i] > (q3 + 1.5 * iqr))),data[i].mean(),data[i])
        
        freq_dict={}
        drop_text_cols=[]
        if clustering_type.get('Text')=='True':
            dbconn,dbcollection = utils.open_dbconn('Clustering_DataPreprocessing')
            features_created = dbcollection.find({"CorrelationId" :correlationId})[0].get('Frequency_dict')
#            if EnDeRequired :
#                t = base64.b64decode(dbcollection.find({"CorrelationId" :correlationId})[0].get('Frequency_dict')) #DE55......................................
#                features_created = eval(EncryptData.DescryptIt(t))
#            else:
#                features_created = dbcollection.find({"CorrelationId" :correlationId})[0].get('Frequency_dict')
            
            for col in text_cols:
                data_features = dbcollection.find({"CorrelationId" :correlationId}) 
                #features_created = data_features[0].get('Frequency_dict')
                text_cols = data_features[0].get('Final_Text_columns')
                text_cols = list(set(text_cols)-set(text_cols_drop))
                data=data[text_cols]
                drop_text_cols = data_features[0].get('Dropped_Text_Cols')
                if text_cols_drop!=text_cols:
                    if len(text_cols)!=0:
                            for col in text_cols:
                                data[col] = data[col].astype(str)
                    for col in text_cols:
                        freq_most = features_created[col]['freq_most'] #remove most frequent words
                        freq_most = list(freq_most)
                        data[col]=data[col].apply(lambda x: " ".join(x for x in x.split() if x not in freq_most))
                        freq_rare = features_created[col]['freq_rare']  #remove least frequent words
                        freq_rare = list(freq_rare) #list of those words
                        data[col] = data[col].apply(lambda x: " ".join(x for x in x.split() if x not in freq_rare))
                
            for x in text_cols:
                data[col] = data[col].astype(str)     
                data[x] = data[x].apply(Textclassification.text_process,args=[True,True,False,[]])
                
            data.replace(r'^\s*$',np.nan,regex=True,inplace=True)
            null_values_df=data.isnull().mean()*100
            null_values_df_20=null_values_df[null_values_df>20]
            drop_text_cols=list(null_values_df_20.index.values)
                
           
                
            if drop_text_cols:
                data.drop(drop_text_cols,axis=1,inplace=True)
                text_cols = list(set(text_cols) - set(drop_text_cols))
            
            if data.empty:
                    message="Data is empty. Either your data contained special characters or numbers or it got dropped during preprocessing"
                    raise Exception()
        
        if clustering_type.get('Non-Text')=='True':  
            try:
                lencm,_,Lenc_cols,_ = utils.get_pickle_file(correlationId,FileType='LE')
                LEcols=[]
                for x in category_cols:
                    for y in Lenc_cols:
                        if x==y.rstrip('_L'):
                            LEcols.append(x)
            
                if len(LEcols)>0:            
                    data,data_le,_ = DataEncoding.Label_Encode_Dec_modified(lencm,data,LEcols)
                    data.drop(LEcols,axis=1,inplace=True)
                    #encoders.update({'LE':{lencm:Lenc_cols}})
                else:
                    pass
            except Exception as e:
                error_encounterd = str(e.args[0])
        
        #data=data[~data.isin([np.nan, np.inf, -np.inf]).any(1)].reset_index(drop=True)
        if clustering_type.get('Text')=='True' and len(text_cols)!=0:
                data.reset_index(inplace=True, drop=True)
                data['All_Text'] = data[list(set(data.columns) and set(text_cols))].astype(str).apply(' '.join, axis=1)
                if data['All_Text'].isnull().sum()>0:
                    message="Uploaded Text Columns are empty. Hence, Text Clustering is not possible."
                    raise Exception()
                    #return utils.updQdb(correlationId,'E',"Uploaded Text Columns are empty. Hence, Text Clustering is not possible.",pageInfo,userId,UniId=Eval_Id,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)              
                data.drop(list(set(data.columns) and set(list(text_cols))),inplace=True,axis=1)
                vectorizer=utils.load_vectorizer(correlationId,'Tfidf Vectorizer')
                X=vectorizer.transform(data['All_Text'])
                
                
                
            
            
        MLDL_Model,_,ProblemType,traincols = utils.get_pickle_file(correlationId,modelName,'MLDL_Model')
         
        #MLDL_Model =  MLDL_Model.best_estimator_
        
        if clustering_type.get('Text')=='True':
            test_data=X
        elif clustering_type.get('Non-Text')=='True':
            test_data = data[traincols]
        else:
             message="Data is not available"
             raise Exception()
           
        utils.updQdb(correlationId, 'P', '70', 'Evaluate_API',userId,modelName,problemType='Clustering',UniId=Eval_Id,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)  
            
           
        #predict_fn = lambda x: MLDL_Model.predict(x)  
        if modelName=='KMeans':
            predict_fn = int(MLDL_Model.predict(test_data)[0])
        elif modelName=='DBSCAN' or modelName=='Agglomerative':
            if clustering_type.get('Text')=='True':
                predict_fn = int(MLDL_Model.predict(test_data.toarray()))
            else:
                predict_fn = int(MLDL_Model.predict(test_data.values))
            
            
        if mapping:
            if modelName=='DBSCAN':
                mapping['Cluster -1']='Your data has not been able to assign to a cluster. Model returned value of -1'
            keys=[eval(i.split(' ')[1]) for i in list(mapping.keys())]
            values=mapping.values()
            map_dict=dict(zip(keys,values))
            predict_fn=map_dict.get(predict_fn)
        utils.updQdb(correlationId, 'C', '100', 'Evaluate_API',userId,modelName,problemType='Clustering',UniId=Eval_Id,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
    
        dbconn, dbcollection = utils.open_dbconn("Clustering_EvalTestResults")  
        
        dbcollection.insert_many([{"CorrelationId":correlationId,
                                        "UniId": Eval_Id,
                                        "BulkTest" : bulk,
                                        "Model":modelName,
                                       
                                        "ProblemType": ProblemType,
                                        "Predictions":predict_fn,
                                        
                                        "CreatedBy":userId,
                                        "CreatedOn":str(datetime.datetime.now()),
    				                    "DCUID": "null" if DCUID==None else DCUID,
                                        "ClientID":"null" if ClientID==None else ClientID,
                                        "ServiceID":"null" if ServiceID==None else ServiceID,
                                        "message":message
                                       # "Predicted_Value_TC": None if ProblemType!='Text_Classification' else predicted_value_TC[0],
                                        #"Predict_Probability_TC": None if ProblemType!='Text_Classification' else predict_fn_TC[0]
                                        }])         
    except Exception as e:
        utils.updQdb(correlationId,'E',str(e.args),pageInfo,userId,UniId=Eval_Id,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
        utils.logger(logger,correlationId,'ERROR','Trace')
        dbconn, dbcollection = utils.open_dbconn("Clustering_EvalTestResults")  
        
        dbcollection.insert_many([{"CorrelationId":correlationId,
                                        "UniId": Eval_Id,
                                        "BulkTest" : bulk,
                                        "Model":modelName,
                                       
                                        "ProblemType": 'Clustering',
                                        "Predictions":[],
                                        
                                        "CreatedBy":userId,
                                        "CreatedOn":str(datetime.datetime.now()),
    				                    "DCUID": "null" if DCUID==None else DCUID,
                                        "ClientID":"null" if ClientID==None else ClientID,
                                        "ServiceID":"null" if ServiceID==None else ServiceID,
                                        "message":str(e.args[0] if not message else message)
                                       # "Predicted_Value_TC": None if ProblemType!='Text_Classification' else predicted_value_TC[0],
                                        #"Predict_Probability_TC": None if ProblemType!='Text_Classification' else predict_fn_TC[0]
                                        }])      
        #utils.save_Py_Logs(logger,correlationId)
        #raise Exception (e.args[0])        
    else:
        utils.logger(logger,correlationId,'INFO',('\n'+"Model Training completed for correlation Id :"+str(correlationId))) 
        utils.save_Py_Logs(logger,correlationId)  
        
    
        
        
        
        
        
        
        
        
