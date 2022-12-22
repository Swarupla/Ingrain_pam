# -*- coding: utf-8 -*-
"""
Created on Wed Apr 24 12:09:42 2019

@author: sravan.kumar.tallozu
"""
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
from SSAIutils import utils
import pandas as pd
import datetime
import uuid
import sys
import base64
import json
from SSAIutils import EncryptData
from pandas import Timestamp

def main(correlationId,uploadId,pageInfo,userId,filepath):
    
    try:
        logger = utils.logger('Get',correlationId)
        utils.updQdb(correlationId,'P','10',pageInfo,userId,UniId=uploadId) 
        EnDeRequired = utils.getEncryptionFlag(correlationId)
        utils.logger(logger,correlationId,'INFO',('WF Ingested : '+"Process initiated at :"+ str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        
        if filepath:
            if filepath.endswith('.csv') or filepath.endswith('.csv.enc'):
                data_t = utils.read_csv(filepath)
            elif filepath.endswith('.xlsx') or filepath.endswith('.xlsx.enc'):    
                data_t = utils.read_excel(filepath)
            else:
                utils.updQdb(correlationId,'E','Please upload correct file',pageInfo,userId,UniId=uploadId) 
                utils.save_Py_Logs(logger,correlationId)
                return 
        if data_t.shape[0]>99:
            utils.updQdb(correlationId,'E','Number of records are more than 100. Please upload file with less records',pageInfo,userId,UniId=uploadId)            
            utils.save_Py_Logs(logger,correlationId)
            return  
        if data_t.shape[0]<=1:
            utils.updQdb(correlationId,'E','Number of records are less than or equal to 1. Please upload file with more records',pageInfo,userId,UniId=uploadId)            
            utils.save_Py_Logs(logger,correlationId)
            return 
            
        if not data_t.empty:
           data_cols = list(data_t.columns)
        
        utils.updQdb(correlationId,'P','30',pageInfo,userId,UniId=uploadId)          
        
        dbconn,dbcollection = utils.open_dbconn("ME_FeatureSelection")
        data_json = dbcollection.find({"CorrelationId" :correlationId})    
        FE_data = data_json[0].get('FeatureImportance')
        
        FS_cols = list()       
        for key,value in FE_data.items():
            if value.get('Selection') == "True":
                FS_cols.append(key)
                
            
        problemTypeflag = data_json[0].get('NLP_Flag')
        if isinstance(problemTypeflag,type(None)):
            problemTypeflag=False
        clustering_flag = data_json[0].get("Clustering_Flag")
        if isinstance(clustering_flag,type(None)):
            clustering_flag=True

        if problemTypeflag:
            if clustering_flag:
                columns_to_remove = data_json[0].get('Cluster_Columns') 
            elif not clustering_flag:
                columns_to_remove = ['All_Text']
                
            list_text_cols = data_json[0].get('Final_Text_Columns')
            dbconn,dbcollection = utils.open_dbconn("DE_DataProcessing")
            data_json = list(dbcollection.find({"CorrelationId" :correlationId}))
            if EnDeRequired :
                t = base64.b64decode(data_json[0].get('DataModification'))
                data_json[0]['DataModification']  =  eval(EncryptData.DescryptIt(t))    
            text_dict = data_json[0].get('DataModification',{}).get('TextDataPreprocessing')
            if text_dict !=None and text_dict.get('TextColumnsDeletedByUser') is not None:
                text_cols_drop=text_dict.get('TextColumnsDeletedByUser')
                list_text_cols = list_text_cols+text_cols_drop
            FS_cols = FS_cols+list_text_cols
            FS_cols = list(set(FS_cols)-set(columns_to_remove))    
            
            
        dbconn,dbcollection = utils.open_dbconn("PS_BusinessProblem")
        data_json = dbcollection.find({"CorrelationId" :correlationId})
        target_variable = data_json[0].get('TargetColumn')
        #print("set(data_cols) :::", set(data_cols)) 
        FS_cols = list(set(FS_cols)-set([target_variable])) 
        dbconn,dbcollection = utils.open_dbconn('DE_AddNewFeature')
        data_json = dbcollection.find({"CorrelationId":correlationId})
        new_features = []
        dbconn.close()
        try:
            new_features = data_json[0].get("Features_Created") 
        except:
            new_features = []               
        if len(new_features)>0:
            FS_cols = list(set(FS_cols)-set(new_features))
             
        #print("set(FS_cols) :::", set(FS_cols))
        if not set(FS_cols).issubset(set(data_cols)):    
            utils.updQdb(correlationId,'I','Selected column should be present in the dataset',pageInfo,userId,UniId=uploadId)            
            utils.save_Py_Logs(logger,correlationId)
            return
        
#        if len(FS_cols)/len(data_cols) < 0.6 :
        if len(data_cols)/len(FS_cols) < 0.6 :            
            utils.updQdb(correlationId,'I','Not enough columns to perform prediction',pageInfo,userId,UniId=uploadId)            
            utils.save_Py_Logs(logger,correlationId)
            return        
        
        utils.updQdb(correlationId,'P','50',pageInfo,userId,UniId=uploadId)  
        
        #Fetch Column Unique values
        colunivals,dtypes = utils.getUniqueValues(data_t) 
        
        #Upload data to WF_IngestedData
        chunks,filesize= utils.file_split(data_t)
        dbconn,dbcollection = utils.open_dbconn("WF_IngestedData")
        
        utils.updQdb(correlationId,'P','70',pageInfo,userId,UniId=uploadId)      
        
        for chi in range(len(chunks)):
            Id=str(uuid.uuid4())
            data_json = chunks[chi].to_json(orient='records',date_format='iso', date_unit='s')  
            if EnDeRequired:                                  #EN555.................................
                data_json = EncryptData.EncryptIt(data_json)
                colunivals = EncryptData.EncryptIt(str(colunivals))
            dbcollection.insert_many([{ "_id" : Id,
                                     "CorrelationId":correlationId,
                                     "UploadId" : str(uploadId),
                                     "InputData":data_json,                                 
                                     "ColumnUniqueValues":colunivals,
                                     "DataTypes":dtypes,
                                     "PageInfo":pageInfo,
                                     "CreatedByUser":userId,
                                     "CreatedOn":str(datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S'))
    
                                     }])
        dbconn.close()        
    
        utils.updQdb(correlationId,'C','100',pageInfo,userId,UniId=uploadId)          
    except Exception as e:
#        dbproconn.close()
        utils.updQdb(correlationId,'E',str(e.args),pageInfo,userId,UniId=uploadId)
        utils.logger(logger,correlationId,'ERROR','Trace',str(None))
        utils.save_Py_Logs(logger,correlationId)
    else:
        utils.logger(logger,correlationId,'INFO',('WF IngestData : '+"Process completed successfully at"++ str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        utils.save_Py_Logs(logger,correlationId)
        return filesize
    
    
    
    
    
    
    
    
