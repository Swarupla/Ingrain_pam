# -*- coding: utf-8 -*-
"""
Created on Mon Jun 15 10:15:55 2020

@author: saurav.b.mondal
"""

import warnings
warnings.filterwarnings("ignore")
import pandas as pd
# Laoding libraries - ML related

from sklearn.feature_extraction.text import TfidfVectorizer
#from sklearn.metrics.pairwise import cosine_similarity
#from sklearn import cluster
from sklearn import metrics
import numpy as np
from SSAIutils import utils
#import matplotlib.pyplot as plt
from sklearn.cluster import DBSCAN
import time
#from sklearn.datasets import make_blobs
from sklearn.preprocessing import StandardScaler
from collections import Counter
from Models import TopicModelling                                 
from main import EncryptData
import json 
from sklearn.linear_model import LogisticRegression as LogReg




def DBSCAN_func(X):
   
    
    
        # Starting a tally of total iterations
    n_iterations = 0
    
    eps_space=[0.1,0.2,0.3,0.4,0.5,0.6,0.7,0.8]
    min_samples_space=[10,20,30,40,50]
    #max_clust=100
    #min_clust=2
    dbscan_clusters = []
    cluster_count   = []
    clst_count=[]
        # Looping over each combination of hyperparameters
    for eps_val in eps_space:
        for samples_val in min_samples_space:
            
            dbscan_grid = DBSCAN(eps = eps_val,
                                 min_samples = samples_val)
    
    
            # fit_transform
            #cosine_distance = cosine_similarity(X.to_numpy())
            #bespoke_distance = np.abs(np.abs(cosine_distance)-1)
            clusters = dbscan_grid.fit_predict(X)
            #print(clusters)
    
    
            # Counting the amount of data in each cluster
            cluster_count = Counter(clusters)
    
    
            # Saving the number of clusters
            #n_clusters = sum(abs(pd.np.unique(clusters))) - 1
            
            core_samples_mask = np.zeros_like(dbscan_grid.labels_, dtype=bool)
            core_samples_mask[dbscan_grid.core_sample_indices_] = True
            labels = dbscan_grid.labels_
            n_clusters = len(set(labels)) - (1 if -1 in labels else 0)
            
            print(n_clusters)
            
            
    
            
            # Increasing the iteration tally with each run of the loop
            n_iterations += 1
    
    
            # Appending the lst each time n_clusters criteria is reached
            #if n_clusters >= min_clust and n_clusters <= max_clust:
            if n_clusters>=2:
                    dbscan_clusters.append([eps_val,
                                        samples_val,
                                        n_clusters,metrics.silhouette_score(X, clusters)])
                    clst_count.append(cluster_count)
            else:
                pass
    
    
            
    
    df=pd.DataFrame(dbscan_clusters,columns=['eps_val','samples_val','n_clusters','Silehoutte Score'])
    
    
    try:
        dbscan=DBSCAN(eps = df.iloc[df['Silehoutte Score'].idxmax()].loc['eps_val'],min_samples = df.iloc[df['Silehoutte Score'].idxmax()].loc['samples_val']).fit(X)
    except:
        
        raise Exception('Number of Clusters is coming as 0 due to the presence of a lot of Noise Points. The DBSCAN Algorithm is unable to group together any points as a result of the presence of a lot of noise. Kindly change/update the quality of your input data.')

    return dbscan,df['Silehoutte Score'].max()

def main(correlationId,modelName,pageInfo,userId,UniId=None,ClientID=None,DCUID=None,ServiceID=None,userdefinedmodelname=None,EnDeRequired=None):
 try:
     logger = utils.logger('Get',correlationId) 
     if EnDeRequired == None or not isinstance(EnDeRequired,bool):
         raise Exception("Encryption Flag is a mandatory field")
     start=time.time()
     dbconn, dbcollection = utils.open_dbconn("Clustering_DE_PreProcessedData")
     data = utils.data_from_chunks(correlationId,'Clustering_DE_PreProcessedData')
     dbconn.close()
     utils.updQdb(correlationId, 'P', '20', pageInfo, userId,modelName,problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname) 
       

     
     dbconn, dbcollection = utils.open_dbconn("Clustering_IngestData")
     data_json = list(dbcollection.find({"CorrelationId": correlationId}))
     dbconn.close()

     n_grams=tuple(data_json[0].get('Ngram'))
     
     try:
         language = data_json[0].get('Language').lower() #'german'
     except Exception:
        language = 'english'

     pType='Clustering'
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
         X=data
     utils.updQdb(correlationId, 'P', '40', pageInfo, userId,modelName,problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)       

     if clustering_type.get('Text')=='True':
         X_train=X
     elif  clustering_type.get('Non-Text')=='True':
         X_train = pd.DataFrame(StandardScaler().fit_transform(X),columns=X.columns)


     clf = LogReg(random_state=0)
# Compute DBSCAN
     db,sil_score = DBSCAN_func(X_train)
     core_samples_mask = np.zeros_like(db.labels_, dtype=bool)
     core_samples_mask[db.core_sample_indices_] = True
     labels = db.labels_
     if clustering_type.get('Text')=='True':
         _,X_test,_,Y_test = utils.train_test_split_utils(X_train.toarray(),labels, test_size=0.3, random_state=50, stratify=None)
         clf.fit(X_train.toarray(),labels)
         accuracy_score = clf.score(X_test,Y_test)
         utils.save_file(clf,modelName,pType,correlationId,pageInfo,userId,['All_Text'],'MLDL_Model')
     else:
         _,X_test,_,Y_test = utils.train_test_split_utils(X_train.values,labels,test_size=0.3, random_state=50, stratify=None)
         clf.fit(X_train.values,labels)
         accuracy_score = clf.score(X_test,Y_test)
         utils.save_file(clf,modelName,pType,correlationId,pageInfo,userId,list(X.columns),'MLDL_Model')
     utils.updQdb(correlationId, 'P', '60', pageInfo, userId,modelName,problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)

# Number of clusters in labels, ignoring noise if present.
     #n_clusters_ = len(set(labels)) - (1 if -1 in labels else 0)
     n_clusters_ = len(set(labels))
     n_noise_ = list(labels).count(-1)
     utils.updQdb(correlationId, 'P', '80', pageInfo, userId,modelName,problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
     print('Estimated number of clusters: %d' % n_clusters_)
     print('Estimated number of noise points: %d' % n_noise_)
     print("Silhouette Coefficient: %0.3f"
      % metrics.silhouette_score(X_train, labels))
     if clustering_type.get('Text')=='True':  
         utils.save_file(clf,modelName,pType,correlationId,pageInfo,userId,['All_Text'],'MLDL_Model')
     else:
         utils.save_file(clf,modelName,pType,correlationId,pageInfo,userId,list(X.columns),'MLDL_Model')
     print('DBSCAN Model done') 
     model_metrics={"Silhouette Coefficient":round(metrics.silhouette_score(X_train, labels),2),
              "Clusters":n_clusters_,
              "Noise_points":n_noise_,
              "accuracy_score": round(accuracy_score,2)}
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
         assigned_clusters_list = list(labels)
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
         
         assigned_clusters_list = list(labels)
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
     utils.updQdb(correlationId, 'C', '100', pageInfo, userId,modelName='DBSCAN',problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)  
 except Exception as e:
        utils.updQdb(correlationId,'E',str(e.args),pageInfo,userId,modelName='DBSCAN',problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
        utils.logger(logger,correlationId,'ERROR','Trace')
        #utils.save_Py_Logs(logger,correlationId)
        raise Exception (e.args[0])        
 else:
     utils.logger(logger,correlationId,'INFO',('\n'+"Model Training completed for correlation Id :"+str(correlationId))) 
     utils.save_Py_Logs(logger,correlationId)         
          

     
     
     
