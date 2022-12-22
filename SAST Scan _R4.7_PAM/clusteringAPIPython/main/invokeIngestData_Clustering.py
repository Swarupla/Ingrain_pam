# -*- coding: utf-8 -*-
"""
Created on Fri Jun  5 10:26:16 2020

@author: saurav.b.mondal
"""
import sys
import os
#sys.path.insert(0,os.getcwd())
import pandas as pd
from SSAIutils import utils
#from SSAIutils import encryption
import pandas as pd
import json
from datetime import datetime
import requests
from urllib.parse import urlencode
from collections import ChainMap
import warnings
warnings.filterwarnings("ignore")
import string
import numpy as np
import random
from main import Data_Ingestion_API
from main import EncryptData


    
def main(correlationId,pageInfo,userId,UniId=None,ClientID=None,DCUID=None,ServiceID=None,userdefinedmodelname=None):
    try:
        logger = utils.logger('Get', correlationId)
        errFlag = False
        IngestData_json,allparams = utils.getRequestParams(correlationId,UniId)
        IngestData_json = eval(IngestData_json)
        #utils.updQdb(correlationId, 'P', '5', pageInfo, userId,UniId)
        utils.updQdb(correlationId,'P',"20",pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)          
        
        #utils.logger(logger, correlationId, 'INFO', str(IngestData_json))
        parent = IngestData_json['Parent']
        mapping = IngestData_json['mapping']
        mapping_flag = IngestData_json['mapping_flag']
        ClientUID = IngestData_json['ClientUID']
        DeliveryConstructUId = IngestData_json['DeliveryConstructUId']
        
        insta_id = ''
        auto_retrain = False
        lastDateDict = {}
        if IngestData_json['Flag'] == "AutoRetrain":
            auto_retrain = True
        if 'DataSetUId' in allparams:
            DataSetUId = allparams['DataSetUId']
        else:
            DataSetUId = False
        MappingColumns = {}
        utils.updQdb(correlationId,'P',"40",pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)          
        if DataSetUId:
            message = utils.update_ps_ingestdata(DataSetUId,correlationId,pageInfo,userId)
            if message == "Ingestion completed":
                utils.logger(logger, correlationId, 'INFO', ('Ingesting_Data' + '\n' + 'Source Type :' + str(message)))
                utils.updQdb(correlationId, 'C', '100', pageInfo, userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
            else:
                utils.logger(logger, correlationId, 'INFO', ('Ingesting_Data' + '\n' + 'Source Type :' + str(message)))
                utils.updQdb(correlationId, 'E', '100', pageInfo, userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
        else:
            if mapping_flag == 'False':
                #utils.updQdb(correlationId, 'P', '10', pageInfo, userId,uniId)
                
                MultiSourceDfs,MappingColumns,lastDateDict,errFlag =  Data_Ingestion_API.Read_Data(correlationId,pageInfo,userId,UniId,ClientID,DCUID,ServiceID,userdefinedmodelname,IngestData_json,False)
                MultiSourceDfs['VDSInstaML']={}
                if errFlag==True:
                    raise Exception("No. of records less than or equal to 19. Please validate the data.")
    
                message = utils.save_data_multi_file(correlationId, pageInfo, userId, UniId,MultiSourceDfs, parent, mapping,
                                                     mapping_flag, ClientUID, DeliveryConstructUId, insta_id, auto_retrain,
                                                     datapre=None, lastDateDict=lastDateDict,MappingColumns= None)   
                if pageInfo=='wordcloud':
                    utils.updQdb(correlationId,'P',"95",pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
                else:
                    utils.updQdb(correlationId,'C',"100",pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)          
    except Exception as e:
        if(errFlag==False):
            utils.updQdb(correlationId,'E',str(e.args),pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
        if str(e.args[0]).__contains__('Data is not available for your selection'):
            utils.logger(logger, correlationId, 'INFO',str("Data is not available for your selection"))
        else:
            utils.logger(logger, correlationId, 'ERROR', 'Trace')
        
        raise Exception (e.args[0])        
    else:
        utils.logger(logger,correlationId,'INFO',('\n'+"Invoke Ingest Data completed for correlation Id :"+str(correlationId))) 
        utils.save_Py_Logs(logger,correlationId)         
    
               

#main(sys.argv[1],sys.argv[2],sys.argv[3],sys.argv[4],sys.argv[5],sys.argv[6],sys.argv[7],sys.argv[8],sys.argv[9],sys.argv[10])
            
                        
            
            
    
            
   