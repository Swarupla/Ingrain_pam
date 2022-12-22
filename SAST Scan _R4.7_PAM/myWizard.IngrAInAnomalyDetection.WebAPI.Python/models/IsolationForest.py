# -*- coding: utf-8 -*-
"""
Created on Fri Aug 12 10:53:32 2022

@author: harsh.nandedkar
"""
import warnings
warnings.filterwarnings("ignore")
import pandas as pd
# Laoding libraries - ML related
from sklearn.feature_extraction.text import TfidfVectorizer
import time
from sklearn import cluster
from sklearn import metrics
#from sklearn.cluster import KMeans
import numpy as np
from SSAIutils import utils
#import matplotlib.pyplot as plt
#from sklearn.cluster import MiniBatchKMeans
#from Models import TopicModelling                                 
from SSAIutils import EncryptData
from sklearn.ensemble import IsolationForest
import json
import uuid
from sklearn.metrics import mean_squared_error

def main(correlationId,modelName,pageInfo,userId,requestId,problemtype):
 try:
     start=time.time()
     EnDeRequired = True
     logger = utils.logger('Get',correlationId) 
     if EnDeRequired == None or not isinstance(EnDeRequired,bool):
         raise Exception("Encryption Flag is a mandatory field")
     dbconn, dbcollection = utils.open_dbconn("DE_PreProcessedData")
     data =utils.data_from_chunks(correlationId,'DE_PreProcessedData')
     dbconn.close()
     utils.updQdb(correlationId, 'P', '20', pageInfo, userId,modelName=modelName, problemType=problemtype, UniId=None,retrycount=3,Incremental=False,requestId=requestId)
     pType='Regression'
     featureParams = utils.getFeatureSelectionVariable(correlationId)
     selectedColumn =utils.getSelectedColumn(featureParams["selectedFeatures"],data.columns)
     targetVar = utils.getTargetVariable(correlationId)
     trainData = data[selectedColumn]
     targetData= data[targetVar]
     X_train,X_test,Y_train,Y_test =utils.train_test_split_utils(trainData,targetData,test_size=0.2,
                                            random_state=50, stratify=None)  
     
     model=IsolationForest(n_estimators=1000,max_samples='auto',contamination=0.2,random_state=42)
     
     
    
     model.fit(X_train)

     print(model.get_params())
     #target='class'
     
     anomaly_values=list(model.predict(X_test))
         
     #data_json = EncryptData.EncryptIt(X_test)
     
     utils.updQdb(correlationId, 'P', '40', pageInfo, userId,modelName=modelName, problemType=problemtype, UniId=None,retrycount=3,Incremental=False,requestId=requestId)
 
       
   
     anamoly=['value']
     anamoly.append({"value":anomaly_values})
     
     dbconn, dbcollection = utils.open_dbconn("PS_BusinessProblem")
     data_json = list(dbcollection.find({"CorrelationId": correlationId}))
     dbconn.close()
     if len(data_json)!=0:
         UniqueIdentifir = data_json[0]['TargetUniqueIdentifier']
    
     utils.save_file(model,modelName,pType,correlationId,pageInfo,userId,list(X_train.columns),'MLDL_Model')
     test_pred = model.predict(X_test)
     multioutput=None
     if not multioutput:
         error = mean_squared_error(Y_test, test_pred)


         
     
     utils.updQdb(correlationId, 'P', '80', pageInfo, userId,modelName=modelName, problemType=problemtype, UniId=None,retrycount=3,Incremental=False,requestId=requestId)

     #utils.save_data(correlationId, pageInfo, userId, 'Clustering_ViewTrainedData',data=view_data,modelname=modelName)
     end=time.time()                                         
     dbconn,dbcollection = utils.open_dbconn('SSAI_RecommendedTrainedModels')
     Id = str(uuid.uuid4())
     dbcollection.update_many({"CorrelationId"     : correlationId,"ModelName":modelName},
                            { "$set":{
                               "_id": Id,                            
                               "CorrelationId"     : correlationId,
                               "pageInfo"          : pageInfo,
                               "CreatedByUser"         : userId,
                               "modelName":modelName,
                               "RunTime":round(end-start,2),
                               "ProblemType": problemtype,
                               "Version" : 0,
                               "y_axis":list(data[targetVar]),
                               "UniqueIdentifir":UniqueIdentifir,
                               "r2ScoreVal": {"error_rate": error },
                              # "Anomaly": anamoly.append({"value":anomaly_values})
                               "Anomalyvalues": str(anomaly_values)
                              

                               }},upsert=True)
     dbconn.close()       
     utils.updQdb(correlationId, 'C', '100', pageInfo, userId,modelName=modelName, problemType=problemtype, UniId=None,retrycount=3,Incremental=False,requestId=requestId) 
 except Exception as e:
        utils.updQdb(correlationId,'E',str(e.args),pageInfo,userId,modelName=modelName,problemType=problemtype,UniId=None,retrycount=3,Incremental=False,requestId=requestId)
        utils.logger(logger,correlationId,'ERROR','Trace')
        #utils.save_Py_Logs(logger,correlationId)
        raise Exception (e.args[0])        
 else:
     utils.logger(logger,correlationId,'INFO',('\n'+"Model Training completed for correlation Id :"+str(correlationId))) 
     utils.save_Py_Logs(logger,correlationId)         
    