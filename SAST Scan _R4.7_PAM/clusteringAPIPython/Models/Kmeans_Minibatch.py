# -*- coding: utf-8 -*-
"""
Created on Fri Jun 12 05:09:21 2020

@author: saurav.b.mondal
"""

import warnings
warnings.filterwarnings("ignore")
import pandas as pd
# Laoding libraries - ML related
from sklearn.feature_extraction.text import TfidfVectorizer
import time
from sklearn import cluster
from sklearn import metrics
from sklearn.cluster import KMeans
import numpy as np
from SSAIutils import utils
#import matplotlib.pyplot as plt
from sklearn.cluster import MiniBatchKMeans
from Models import TopicModelling                                 
from main import EncryptData
import json

def main(correlationId,modelName,n_cluster,pageInfo,userId,UniId=None,ClientID=None,DCUID=None,ServiceID=None,userdefinedmodelname=None,EnDeRequired=None):
 try:
     start=time.time()
     logger = utils.logger('Get',correlationId) 
     if EnDeRequired == None or not isinstance(EnDeRequired,bool):
         raise Exception("Encryption Flag is a mandatory field")
     dbconn, dbcollection = utils.open_dbconn("Clustering_DE_PreProcessedData")
     data = utils.data_from_chunks(correlationId,'Clustering_DE_PreProcessedData')
     dbconn.close()
     utils.updQdb(correlationId, 'P', '20', pageInfo, userId,modelName='KMeans',problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname) 

     dbconn, dbcollection = utils.open_dbconn("Clustering_IngestData")
     data_json = list(dbcollection.find({"CorrelationId": correlationId}))
     dbconn.close()

     n_grams=tuple(data_json[0].get('Ngram'))

     try:
         language = data_json[0].get('Language').lower() #'german'
     except Exception:
        language = 'english'

     pType=list(data_json[0].get('ProblemType').keys())[0]
     clustering_type=data_json[0]['ProblemType']['Clustering']
     if clustering_type.get('Text')=='True':
         vectorizer=TfidfVectorizer()
         try:
             X = vectorizer.fit_transform(data['All_Text'])
             utils.save_vectorizer(correlationId,'Tfidf Vectorizer',vectorizer)
         except KeyError:
             utils.updQdb(correlationId,'E',"The Uploaded Data does not contain any Text Columns. Hence, Text Clustering is not possible. Try choosing Clustering as Non-Text instead",pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)              
             raise Exception("The Uploaded Data does not contain any Text Columns. Hence, Text Clustering is not possible. Try choosing Clustering as Non-Text instead")
     else:
         print(data.dtypes)
         X=data.astype('float64')
         
         
     
     utils.updQdb(correlationId, 'P', '40', pageInfo, userId,modelName='KMeans',problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname) 
       
     if n_cluster==1:
        
        ff=[]
        if data.shape[0]*data.shape[1]<=500000:
       
            for i in range(2,15):
                print("Cluster is", i)
                #kmeans=cluster.KMeans(n_clusters=i,random_state=42)
                kmeans = MiniBatchKMeans(n_clusters=i,random_state=42,batch_size=1000,max_iter=300)
                kmeans.fit(X)
                labels=kmeans.labels_
                silhouette_score=metrics.silhouette_score(X, labels, metric='euclidean',random_state=42)
                ff.append(silhouette_score)
                
            optimal_clusters=ff.index(max(ff))+2
        
        else:
            optimal_clusters=7
        #print(optimal_clusters)
        #plt.plot(range(2,15),ff)
        #plt.xlabel("# of Clusters")
        #plt.ylabel("Silhouette Score")
        #plt.axvline(x=optimal_clusters, linestyle='--',ymax=0.95)
        #plt.axhline(y=max(ff),linestyle='--',xmax=optimal_clusters/15)
        #plt.savefig("silhouette_graph.png")
        #with tempfile.TemporaryFile(suffix=".png") as tmpfile:
        #    tmpfile = open("silhouette_graph.png","rb")
        #    tmpfile.seek(0)
        #    silhouette_graph = str(base64.b64encode(tmpfile.read()))
        #silhouette_graph = imageEncoded
     else:
        optimal_clusters=n_cluster
     utils.updQdb(correlationId, 'P', '60', pageInfo, userId,modelName='KMeans',problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname) 
       

        #silhouette_graph = ''
  #################################################################################    
    #kmeans1 = KMeans(optimal_clusters).fit(X.T)
    
     model=MiniBatchKMeans(n_clusters=optimal_clusters,random_state=42,batch_size=1000,max_iter=300).fit(X)
     assigned_clusters =model.labels_
     print(assigned_clusters)
     utils.updQdb(correlationId, 'P', '80', pageInfo, userId,modelName='KMeans',problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname) 
       
     if clustering_type.get('Text')=='True':  
         utils.save_file(model,modelName,pType,correlationId,pageInfo,userId,['All_Text'],'MLDL_Model')
     else:
         utils.save_file(model,modelName,pType,correlationId,pageInfo,userId,list(X.columns),'MLDL_Model')
     print('K Means Model done')
     model_metrics={"Silhouette Coefficient":round(metrics.silhouette_score(X, assigned_clusters),2),
              "Clusters":model.n_clusters}
     end=time.time()
     if clustering_type.get('Text')=='True':  
         print('Here')
         offlineutility = utils.checkofflineutility(correlationId)
         if offlineutility:
            data_LDA = utils.data_from_chunks_offline_utility(corid=correlationId, collection="DataSet_IngestData",lime=None, recent=None,DataSetUId=offlineutility)
         else:
            data_LDA = utils.data_from_chunks(corid=correlationId,collection="Clustering_BusinessProblem")  

         dbconn, dbcollection = utils.open_dbconn("Clustering_DataPreprocessing")
         data_json = list(dbcollection.find({"CorrelationId": correlationId}))
         text_cols=data_json[0].get("Text_columns")
         data_dropped_index=data_json[0].get("data_dropped_indices") 
         data_LDA.drop(index=data_dropped_index,inplace=True)
         data_LDA.reset_index(inplace=True, drop= True)
         dbconn.close()
         
         for x in data_LDA[text_cols].columns:
             data_LDA[x]= TopicModelling.preprocessing(data_LDA[x],language)
             data_LDA.replace(r'^\s*$','We cannot possibly leave this field blank!',regex=True,inplace=True)
             data_LDA.reset_index(inplace=True, drop=True)
         
         if data.shape[0]!=data_LDA.shape[0]:
             raise Exception("Now of rows do not match!")
         data_LDA['All_Text'] = data_LDA[list(set(data_LDA.columns) and set(text_cols))].astype(str).apply(' '.join, axis=1)
         assigned_clusters_list = list(assigned_clusters)
         pred_clusters = pd.DataFrame({"Predicted Clusters":assigned_clusters_list})
    
         data_q = pd.concat([data,pred_clusters], axis=1)
         
         view_data=pd.concat([data_LDA[list(set(data_LDA.columns)-set(['All_Text']))],pred_clusters],axis=1)
         
         topic_dictionary=TopicModelling.main(data_q,num_topics=10,ngram_range=n_grams,modelname=modelName)
         
         if EnDeRequired:
             topic_dictionary = EncryptData.EncryptIt(json.dumps(topic_dictionary))
             
         utils.save_data(correlationId, pageInfo, userId, 'Clustering_ViewTrainedData',data=view_data,modelname=modelName)
     elif clustering_type.get('Non-Text')=='True':
         offlineutility = utils.checkofflineutility(correlationId)
         if offlineutility:
            data_View = utils.data_from_chunks_offline_utility(corid=correlationId, collection="DataSet_IngestData",lime=None, recent=None,DataSetUId=offlineutility)
         else:
            data_View = utils.data_from_chunks(corid=correlationId,collection="Clustering_BusinessProblem")  

#         dbconn, dbcollection = utils.open_dbconn("Clustering_DataPreprocessing")
#         data_json = list(dbcollection.find({"CorrelationId": correlationId}))
#         data_dropped_index=data_json[0].get("data_dropped_indices") 
#         data_View.drop(index=data_dropped_index,inplace=True)
#         data_View.reset_index(inplace=True, drop= True)
#         dbconn.close()
         if data.shape[0]!=data_View.shape[0]:
             raise Exception("Now of rows do not match!")
         
         assigned_clusters_list = list(assigned_clusters)
         pred_clusters = pd.DataFrame({"Predicted Clusters":assigned_clusters_list})
         view_data=pd.concat([data_View,pred_clusters],axis=1)
         utils.save_data(correlationId, pageInfo, userId, 'Clustering_ViewTrainedData',data=view_data,modelname=modelName)
                                              
     dbconn,dbcollection = utils.open_dbconn('Clustering_TrainedModels')
     dbcollection.update_many({"CorrelationId"     : correlationId,"ModelName":modelName},
                            { "$set":{      
                               "CorrelationId"     : correlationId,
                               "pageInfo"          : pageInfo,
                               "CreatedBy"         : userId,
                               #"Text_Null_Columns_Less20":drop_text_cols,
                               "Model Metrics":model_metrics,
                               "Clustering_type":clustering_type,
                               "ModelName":modelName,
                               "DCUID": "null" if DCUID==None else DCUID,
                               "ClientID":"null" if ClientID==None else ClientID,
                               "ServiceID":"null" if ServiceID==None else ServiceID,
                               "Runtime":round(end-start,2),
                               "Topic_dictionary":"null" if clustering_type.get('Text')!='True' else topic_dictionary

                               }},upsert=True)
     dbconn.close()       
     utils.updQdb(correlationId, 'C', '100', pageInfo, userId,modelName='KMeans',problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname) 
 except Exception as e:
        utils.updQdb(correlationId,'E',str(e.args),pageInfo,userId,modelName='KMeans',problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
        utils.logger(logger,correlationId,'ERROR','Trace')
        #utils.save_Py_Logs(logger,correlationId)
        raise Exception (e.args[0])        
 else:
     utils.logger(logger,correlationId,'INFO',('\n'+"Model Training completed for correlation Id :"+str(correlationId))) 
     utils.save_Py_Logs(logger,correlationId)         
    