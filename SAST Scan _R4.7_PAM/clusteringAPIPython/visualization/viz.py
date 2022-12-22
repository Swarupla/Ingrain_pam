# -*- coding: utf-8 -*-
"""
Created on Fri Mar 26 15:56:40 2021

@author: harsh.nandedkar
"""
import numpy as np
from SSAIutils import utils
import pandas as pd
from sklearn.ensemble import RandomForestClassifier
from visualization import wordcloud_api
from sklearn.preprocessing import StandardScaler
import time
from main import EncryptData
import json
import uuid



def main(correlationId,keys,mapping,modelName,userId,pageInfo,problemtype=None,flag=None,UniId=None,ClientID=None,DCUID=None,ServiceID=None,userdefinedmodelname=None,EnDeRequired=None):
    logger = utils.logger('Get',correlationId) 
    if flag==None:
        raise Exception("We cannot have Flag as None Type")
    if problemtype==None:
        raise Exception("Problem Type cannot be none")
    if EnDeRequired == None or not isinstance(EnDeRequired,bool):
         raise Exception("Encryption Flag is a mandatory field")
    
    try:
        if flag==True:
            if problemtype=='Text':
                stopword_list=[]
                max_words=1000
                response_dict={}
                
                start=time.time()
                df=utils.data_from_chunks(corid=correlationId, collection="Clustering_ViewTrainedData",modelName=modelName)
                if set(df['Predicted Clusters'].unique())==set(keys):
                    values=mapping.values()
                    replace_dict=dict(zip(keys,values))
                    df['Predicted Clusters'].replace(replace_dict,inplace=True)
                    if modelName!='DBSCAN':
                        if set(df['Predicted Clusters'].unique())==set(keys):
                            values=mapping.values()
                            replace_dict=dict(zip(keys,values))
                            df['Predicted Clusters'].replace(replace_dict,inplace=True)
                        elif modelName=='DBSCAN':
                            unique_pred=df['Predicted Clusters'].unique()
                            values=mapping.values()
                            replace_dict=dict(zip(keys,values))
                            replace_dict.update(dict.fromkeys(list(set(unique_pred)-set(keys)), "Noise Point. Not Defined"))
                            df['Predicted Clusters'].replace(replace_dict,inplace=True)
              
            else:
              utils.updQdb(correlationId,'E',"There is a mismatch in Mapping. The Mapping keys are not in line with the models predicted clusters.",pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)            
              raise Exception()
            
            frequency_count=df['Predicted Clusters'].value_counts().to_dict()
            for i in df['Predicted Clusters'].unique():
                response=wordcloud_api.gen_wordcloud(correlationId,stopword_list,pageInfo,max_words,flag=True,df=df[df['Predicted Clusters']==i])
                response_dict[i]=response
            
            utils.updQdb(correlationId, 'P', '50', pageInfo, userId,modelName=modelName,problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname) 

            end=time.time()
            response_dict={str(k):v for k,v in response_dict.items()}
            if EnDeRequired:
                    response_dict_encrypted = EncryptData.EncryptIt(json.dumps(response_dict))
                    frequency_count_encrypted=EncryptData.EncryptIt(json.dumps(frequency_count))
            dbconn,dbcollection = utils.open_dbconn('Clustering_Visualization')
            dbcollection.update_many({"CorrelationId"     : correlationId,"ModelType":modelName},
                            { "$set":{      
                               "CorrelationId"     : correlationId,
                               "pageInfo"          : pageInfo,
                               "CreatedBy"         : userId,
                               #"Text_Null_Columns_Less20":drop_text_cols,
                               "Clustering_type":problemtype,
                               "ModelType":modelName,
                               "DCUID": "null" if DCUID==None else DCUID,
                               "ClientID":"null" if ClientID==None else ClientID,
                               "ServiceID":"null" if ServiceID==None else ServiceID,
                               "ModelName":userdefinedmodelname,
                               "Runtime":round(end-start,2),
                               "Visualization_Response":response_dict_encrypted if EnDeRequired else response_dict, 
                               "Frequency_Count":frequency_count_encrypted if EnDeRequired else frequency_count

                               }},upsert=True)
            dbconn.close() 
            utils.updQdb(correlationId, 'C', '100', pageInfo, userId,modelName=modelName,problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname) 

            return 'Success'
            ##WRITE DB INSERTION SCRIPT
        elif flag!=True:
            if problemtype=='Non-Text':
                start=time.time()
                df=utils.data_from_chunks(corid=correlationId, collection="Clustering_DE_PreProcessedData")
                df_1=utils.data_from_chunks(corid=correlationId, collection="Clustering_ViewTrainedData",modelName=modelName)
                df=pd.concat([df,df_1['Predicted Clusters']],axis=1)
                frequency_count=df_1['Predicted Clusters'].value_counts().to_dict()
                frequency_count={'Cluster'+' '+str(k):v for k,v in frequency_count.items()}
                X, y = df.iloc[:,:-1], df.iloc[:,-1]
                X=X.astype('float64')
                ss = StandardScaler()
                df_scaled = pd.DataFrame(ss.fit_transform(X),columns = X.columns)
                clustering_modeluniqueId_viz,model_uniqueflag=utils.model_viz_rf(correlationId,'Clustering',pageInfo)
                if type(clustering_modeluniqueId_viz)!=str:
                    clustering_modeluniqueId_viz=str(uuid.uuid4())
                    clf = RandomForestClassifier(n_estimators=100).fit(np.nan_to_num(df_scaled), y)
                    utils.save_file(clf,'Random Forest Classifier','Clustering',correlationId,pageInfo,userId,list(df_scaled.columns),'Clustering_Viz_Model',clustering_modeluniqueId_viz=clustering_modeluniqueId_viz)
                elif type(clustering_modeluniqueId_viz)==str:
                    clf=utils.get_pickle_file(correlationId,'Random Forest Classifier','Clustering_Viz_Model',clustering_modeluniqueId_viz=clustering_modeluniqueId_viz)
                
                try:
                    data = np.array([clf.feature_importances_, df_scaled.columns]).T
                except:
                    data = np.array([clf[0].feature_importances_, df_scaled.columns]).T
                columns=list(pd.DataFrame(data, columns=['Importance', 'Feature']).sort_values("Importance", ascending=False).head(10).Feature.values)
#               
                df_scaled=pd.concat([df_scaled,df_1['Predicted Clusters']],axis=1)
#                columns = list(pd.DataFrame(data, columns=['Importance', 'Feature']).sort_values("Importance", ascending=False).head(10).Feature.values)
                viz_df = df_scaled[columns+['Predicted Clusters']].melt(id_vars='Predicted Clusters')
                groupby_df=viz_df.groupby(['Predicted Clusters','variable']).agg(['count','mean'])
                
                groupby_df['Index']=[list(x) for x in groupby_df.index]
                groupby_df.reset_index(inplace=True,drop=True)
                #newdf=pd.DataFrame(groupby_df[('Index','')].to_list(),columns=['Cluster_no','Feature_Importance'])
                #groupby_df.iloc[:,1]
                newdf=[]
                for x in groupby_df.columns:
                    newdf.append(groupby_df[x].values)
                #fin=pd.DataFrame(newdf)
                names=['Count','Frequency Values','Name']
                fin=pd.DataFrame.from_dict(dict(zip(names,newdf)))
                fin_df=pd.concat([pd.DataFrame(fin['Name'].to_list(),columns=['Cluster_no','Feature_Importance']),fin],axis=1)
                fin_df.drop(columns=['Name'],inplace=True)
                column_fin=[x for x in fin_df.columns if x!='Cluster_no']
                response_dict={}
                for x in fin_df['Cluster_no'].unique():
                    response_dict['Cluster'+' '+str(x)]={}
                    for y in column_fin:
                        response_dict['Cluster'+' '+str(x)][y]=fin_df[fin_df['Cluster_no']==x][y].tolist()
                
                end=time.time()
                #output_dict = {}
                #for key, value in response_dict.items():
                #    output_dict[int(key)] = value
                utils.updQdb(correlationId, 'P', '50', pageInfo, userId,modelName=modelName,problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname) 

                if EnDeRequired:
                    response_dict_encrypted = EncryptData.EncryptIt(json.dumps(response_dict))
                    frequency_count_encrypted=EncryptData.EncryptIt(json.dumps(frequency_count))
                
                dbconn,dbcollection = utils.open_dbconn('Clustering_Visualization')
                dbcollection.update_many({"CorrelationId"     : correlationId,"ModelType":modelName},
                            { "$set":{      
                               "CorrelationId"     : correlationId,
                               "pageInfo"          : pageInfo,
                               "CreatedBy"         : userId,
                               #"Text_Null_Columns_Less20":drop_text_cols,
                               "Clustering_type":problemtype,
                               "ModelType":modelName,
                               "ModelName":userdefinedmodelname,
                               "DCUID": "null" if DCUID==None else DCUID,
                               "ClientID":"null" if ClientID==None else ClientID,
                               "ServiceID":"null" if ServiceID==None else ServiceID,
                               "Runtime":round(end-start,2),
                               "Visualization_Response":response_dict_encrypted if EnDeRequired else response_dict, 
                               "Frequency_Count":frequency_count_encrypted if EnDeRequired else frequency_count

                               }},upsert=True)
                dbconn.close()
                utils.updQdb(correlationId, 'C', '100', pageInfo, userId,modelName=modelName,problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname) 

                return 'Success'
    except Exception as e:
        utils.updQdb(correlationId,'E',str(e.args),pageInfo,userId,modelName=modelName,problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
        utils.logger(logger,correlationId,'ERROR','Trace')
        #utils.save_Py_Logs(logger,correlationId)
        raise Exception (e.args[0])        
    else:
     utils.logger(logger,correlationId,'INFO',('\n'+"Model Training completed for correlation Id :"+str(correlationId))) 
     utils.save_Py_Logs(logger,correlationId)         
    

            
                