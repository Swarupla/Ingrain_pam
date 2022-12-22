# -*- coding: utf-8 -*-
"""
Created on Sun Aug  2 16:31:20 2020

@author: harsh.nandedkar
"""

import sys
import logging
# SSAI Scripts
from SSAIutils import utils
import pandas as pd
import numpy as np
import time

def main(correlationId,pageInfo,userId,mapping,modelName=None,UniId=None,ClientID=None,DCUID=None,ServiceID=None,userdefinedmodelname=None):
      try:
          logger = utils.logger('Get',correlationId) 
          if modelName==None:
              utils.updQdb(correlationId,'E',"Model Name cannot be blank!",pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)            
              raise Exception()
          if isinstance(mapping,dict) and len(mapping)==0:
              utils.updQdb(correlationId,'E',"Mapping JSON cannot be blank!",pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)            
              raise Exception()
          if not isinstance(mapping,dict):
              utils.updQdb(correlationId,'E',"Mapping JSON datatype is not a dictionary!",pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)            
              raise Exception()
              
          start=time.time()
         
          dbconn, dbcollection = utils.open_dbconn("Clustering_ViewTrainedData")
          data_json = list(dbcollection.find({"CorrelationId": correlationId}))
          dbconn.close()
          
          data = utils.data_from_chunks(corid=correlationId, collection="Clustering_ViewTrainedData",modelName=modelName)
#          dbconn,dbcollection = utils.open_dbconn('Clustering_TrainedModels')
#          data_json = list(dbcollection.find({"CorrelationId": correlationId}))
#          dbconn.close()
          utils.updQdb(correlationId, 'P', '10', pageInfo, userId,modelName=modelName,problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname) 

          keys=[eval(i.split(' ')[1]) for i in list(mapping.keys())]
          if set(data['Predicted Clusters'].unique())==set(keys):
              values=mapping.values()
              replace_dict=dict(zip(keys,values))
              data['Predicted Clusters'].replace(replace_dict,inplace=True)
          if modelName!='DBSCAN':
              if set(data['Predicted Clusters'].unique())==set(keys):
                  values=mapping.values()
                  replace_dict=dict(zip(keys,values))
                  data['Predicted Clusters'].replace(replace_dict,inplace=True)
          elif modelName=='DBSCAN':
              unique_pred=data['Predicted Clusters'].unique()
              values=mapping.values()
              replace_dict=dict(zip(keys,values))
              replace_dict.update(dict.fromkeys(list(set(unique_pred)-set(keys)), "Noise Point. Not Defined"))
              data['Predicted Clusters'].replace(replace_dict,inplace=True)
              
          else:
              utils.updQdb(correlationId,'E',"There is a mismatch in Mapping. The Mapping keys are not in line with the models predicted clusters.",pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)            
              raise Exception()
          end=time.time()
          print('Time Taken: ',end-start)
          utils.save_data(correlationId, pageInfo, userId, 'Clustering_ViewMappedData',filepath=None,data=data,modelname=modelName,datapre = None,mapdata=True)
          utils.updQdb(correlationId, 'C', '100', pageInfo, userId,modelName=modelName,problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname) 
      except Exception as e:
        utils.updQdb(correlationId,'E',str(e.args),pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
        utils.logger(logger,correlationId,'ERROR','Trace')
        #utils.save_Py_Logs(logger,correlationId)
        raise Exception (e.args[0])        
      else:
        utils.logger(logger,correlationId,'INFO',('\n'+"Data Mapping completed for correlation Id :"+str(correlationId))) 
        utils.save_Py_Logs(logger,correlationId)       
              
                  