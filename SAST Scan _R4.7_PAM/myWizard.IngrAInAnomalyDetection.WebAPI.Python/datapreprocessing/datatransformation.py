# -*- coding: utf-8 -*-
"""
Created on Mon Aug  8 15:13:19 2022

@author: k.sudhakara.reddy
"""


import time
from SSAIutils import utils
from datapreprocessing import DataEncoding
#from datapreprocessing import Textclassification
from datapreprocessing import Feature_Importance
import sys
import pandas as pd
import numpy as np
#import spacy
import base64
import json
from SSAIutils import EncryptData
from bson import json_util
from datetime import datetime,timedelta


import pandas
from sklearn import linear_model
from sklearn.preprocessing import StandardScaler
scale = StandardScaler()

from sklearn import preprocessing

          
def fetchProblemType(correlationId):
    dbconn,dbcollection = utils.open_dbconn("DE_DataCleanup")
    data_json = dbcollection.find({"CorrelationId" :correlationId})    
    dbconn.close()
    problemtype = data_json[0].get('Target_ProblemType')
    if problemtype == '1':
        problemtypef = 'Regression'
    elif problemtype == '2':
        problemtypef = 'Classification'
    elif problemtype == '3':
        problemtypef = 'Multi_Class'
    elif problemtype == '4':
        problemtypef = 'TimeSeries'
    return problemtypef
          
def main(correlationId,pageInfo,userId,requestId):
    try:
        start=time.time()
        logger = utils.logger('Get',correlationId) 
        EnDeRequired = True
        if EnDeRequired == None or not isinstance(EnDeRequired,bool):
            raise Exception("Encryption Flag is a mandatory field")
        timeSeries = False                       
        #print("inside main")
        utils.updQdb(correlationId,'P','10',pageInfo,userId,requestId=requestId)  
        utils.logger(logger, correlationId, 'INFO', ('Data Preprocessing started '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
        
        dbconn, dbcollection = utils.open_dbconn("PS_BusinessProblem")
        data_json = list(dbcollection.find({"CorrelationId": correlationId}))
        dbconn.close()
        problemtypef = data_json[0]['ProblemType']
        probtype = data_json[0]['ProblemType']
        if data_json[0]['ProblemType']!="TimeSeries":
            input_columns = data_json[0].get('InputColumns')
        else:
            timeSeries = True
            input_columns = [data_json[0]['TimeSeries']['TimeSeriesColumn']]
        target_variable = data_json[0].get('TargetColumn')
        #print('TARGET VARIABLE IS: ', target_variable)
        uniqueIdentifier = data_json[0].get('TargetUniqueIdentifier')
        
        if uniqueIdentifier!=None:
            data_cols_t = input_columns + [target_variable]+[uniqueIdentifier]
        else:
            data_cols_t = input_columns + [target_variable]
        
        """problem_type = 'Clustering'
        clustering_type=data_json[0]['ProblemType']['Clustering']
        col_selectedbyuser=list(data_json[0].get('Columnsselectedbyuser'))"""

        
        offlineutility = utils.checkofflineutility(correlationId)
        if offlineutility:
            data_F = utils.data_from_chunks_offline_utility(corid=correlationId, collection="DataSet_IngestData",lime=None, recent=None,DataSetUId=offlineutility)
        else:
            data_F = utils.data_from_chunks(corid=correlationId,collection="PS_IngestedData")  

        utils.updQdb(correlationId, 'P', '40', pageInfo, userId,requestId=requestId)
        
        data= data_F[data_cols_t]
        data_cols = data.columns  
        #print("Data Columns after ingestion", data_cols)
        data_dtypes = dict(data.dtypes)
        ndata_cols = data.shape[1]
        datasize = (sys.getsizeof(data)/ 1024) / 1024 
    
        dbconn,dbcollection = utils.open_dbconn("DE_DataCleanup")
        features_data_json = dbcollection.find({"CorrelationId" :correlationId}) 
        dbconn.close()
        Features = features_data_json[0].get('Feature Name') 
        Features1 = features_data_json[0].get('Feature Name')
        
        if EnDeRequired :
            t = base64.b64decode(Features) #DE55......................................
            Features = json.loads(EncryptData.DescryptIt(t), object_hook=json_util.object_hook)
            Features1 = json.loads(EncryptData.DescryptIt(t), object_hook=json_util.object_hook)
        
        for x in Features1.keys():
            if x not in data_cols_t:
                del Features[x]
    
        features_dtypesf = {}
        for key,value in Features.items():        
            d_type = [key1 for key1,value1 in value.get('Datatype').items() if value1 =='True']
            if d_type:    
                    features_dtypesf.update({key:d_type[0]})
            else:
                features_dtypesf.update({key:'ND'})  
                
        
        drop_cols_t=[]   
        text_cols = []
        category_cols=[]
        numerical_cols=[]
        not_defined=[]
        for key5,value5 in features_dtypesf.items():
             if not timeSeries:            
                 if value5 in ['Id','Text','datetime64[ns]']:
                        drop_cols_t.append(key5)
                 if value5 == 'Text':
                        text_cols.append(key5)
                 if value5 =='category':
                      category_cols.append(key5)
                 if value5 =='float64' or value5=='int64':
                      numerical_cols.append(key5)
                 if value5 =='Select Option':
                      not_defined.append(key5)
             if timeSeries:
                if value5 in ['Id']:
                    drop_cols_t.append(key5)
                #if problemtypef=='Text_Classification':
                #    if value5 in ['Id','datetime64[ns]']:
                #        drop_cols_t.append(key5)
        drop_cols_t= list(set(drop_cols_t)- set(text_cols))+not_defined
        #print("Dropped columns after Ingestion aree: ", drop_cols_t)
        #problemtypef=fetchProblemType(correlationId)
        target_datatype=features_dtypesf.get(target_variable)
        #print("Target Datatype is: ", target_datatype)
        if probtype != "TimeSeries":
            if target_datatype=='float64' or target_datatype=='int64':
                problemtypef='Regression'
            elif target_datatype=='datetime64[ns]':
                problemtypef=='TimeSeries'
            
        if drop_cols_t:
            data.drop(drop_cols_t,axis=1,inplace=True)
        
        data=data[list(set(data.columns)- set(text_cols))]

        missing_values=data.columns[data.isnull().any()].tolist() 
        data_dropped_index_1=list(pd.isnull(data).any(1).to_numpy().nonzero())
        missingvalues_dict={}
        for col in list(set(numerical_cols)&set(missing_values)):
            data[col].fillna(data[col].mean(),inplace=True)
            missingvalues_dict[col]=data[col].mean()
        for col in list(set(category_cols)&set(missing_values)):
            data[col].fillna(data[col].mode()[0],inplace=True)
            missingvalues_dict[col]=data[col].mode()[0]
        #print("MISSING VALUES ARE: ",missingvalues_dict)

        utils.updQdb(correlationId, 'P', '60', pageInfo, userId,requestId=requestId)
        for i in numerical_cols:        
                q1 = data[i].quantile(0.25)
                q3 = data[i].quantile(0.75)
                iqr = q3-q1 
                data[i] = np.where(((data[i] < (q1 - 1.5 * iqr)) |(data[i] > (q3 + 1.5 * iqr))),data[i].mean(),data[i])
        



        LEcols=[]
        if category_cols:
            LEcols=category_cols
            x,_,lencm,Lenc_cols = DataEncoding.Label_Encode(data[category_cols], LEcols)
            #print("DATA COLUMNS IN LENC COLS ARE: ",data.columns)
            utils.save_file(lencm,'LE',Lenc_cols,correlationId,pageInfo,userId,FileType='LE')
        
        drop_cols = LEcols
        #print("DROP COLS ARE: ",drop_cols)
        #print("Lenc Cols are: ",LEcols)
        if drop_cols:
                data_t = data.drop(drop_cols,axis=1)
                #print("Data_t columns: ",data_t.columns)
                #print("Data_t columns: ",data_t)
                #print("VALUE OF X is :",x)
                data=pd.concat([data_t,x[Lenc_cols]],axis=1)
                
                #print("Data columns after Label Encoding: ",data.columns)
        else:
            data_t = data
        if probtype != "TimeSeries":
            data2 = data[list(set(data.columns) - set([target_variable]))]
            data_1 = pd.DataFrame(scale.fit_transform(data2),columns=list(data2.columns))
        #print("DATA_1 DF COLUMNS ARE: ",data_1.columns)

        data=data[~data.isin([np.nan, np.inf, -np.inf]).any(1)].reset_index(drop=True)
        if data.isnull().values.any():
            utils.logger(logger,correlationId,'INFO', ('data dtypes'+str(dict(data.isna().sum()))))
            utils.updQdb(correlationId, 'E', "Their are nulls in data, preprocess data, Missing values", pageInfo, userId,requestId=requestId)
            raise Exception ("Their are nulls in data, preprocess data, Missing values")
        

        #print(data.columns)
        feat_imp = {}       
        if probtype != "TimeSeries":
            EstimatedRunTime = "18.303344011306763"
            y_data = data[target_variable]
            x_data = data_1
            Feat_Imp = Feature_Importance.feature_importance(problemtypef,x_data,y_data,data_cols)
        '''CHANGES END HERE'''
        if probtype != "TimeSeries":
            for i,j in Feat_Imp.iterrows():  
                temp = {}
                temp.update({'Selection':'True'})
                temp.update({'Value':j[1]})
                feat_imp.update({j[0]:temp})
            if uniqueIdentifier and not timeSeries:
                temp = {}
                temp.update({'Selection':'True'})
                temp.update({'Value':0})
                #feat_imp.update({uniqueIdentifier:temp})
        dbproconn,dbprocollection = utils.open_dbconn("DE_DataProcessing")
        data_json = list(dbprocollection.find({"CorrelationId" :correlationId}))
        
        if timeSeries:
            smote_F = False
        try:
            smote_F = data_json[0].get('Smote').get('Flag')
        except:
            smote_F=False
                
        if timeSeries:
                interapolationTechnique = "Linear"
        #problemtypef=fetchProblemType(correlationId)
        if target_variable in LEcols:             
            Atarget_variable = target_variable + '_' + 'L' 
        else:
            Atarget_variable = target_variable
            
        if problemtypef == 'TimeSeries':
            EstimatedRunTime = "2.1919496059417725"
            from datapreprocessing import aggregation
            data_t = data
            if uniqueIdentifier and uniqueIdentifier in data_t.columns:
                data_t.drop(uniqueIdentifier,axis=1,inplace=True)
            data_t = aggregation.main(correlationId,data_t,input_columns[0],Atarget_variable,interapolationTechnique)
            data_cols_t = [data_t[list(data_t.keys())[0]].index.name]+list(data_t[list(data_t.keys())[0]].columns)
        
        if timeSeries:
            filesize = utils.save_data_timeseries(correlationId,pageInfo,userId,'DE_PreProcessedData',data_cols_t,data=data_t,datapre=True)
        else:
            filesize = utils.save_data(correlationId,pageInfo,userId,'DE_PreProcessedData',data=pd.concat([x_data,y_data],axis=1),datapre=True)

        utils.updQdb(correlationId, 'P', '80', pageInfo, userId,requestId=requestId)
        dbconn,dbcollection = utils.open_dbconn('ME_FeatureSelection')
        dbcollection.update_many({"CorrelationId"     : correlationId},
                                { "$set":{      
                                   "CorrelationId"     : correlationId,
                                   "pageInfo"          : pageInfo,
                                   "CreatedBy"         : userId,
                                   "Train_Test_Split"  : {"TrainingData": 70},
                                   "Dropped_cols":drop_cols_t,
                                   "Category_columns":category_cols,
                                   "Numerical_columns":numerical_cols,
                                   "Label_Encoded_columns":LEcols,
                                   "Stored_MissingValues":missing_values,
                                   "FeatureImportance": feat_imp,
                                   "Actual_Target":target_variable,
                                   "EstimatedRunTime" : EstimatedRunTime,
                                   "StratifiedSampling": "True"
                                  
    
                                   }},upsert=True)
        dbconn.close()
        
    except Exception as e:
        utils.updQdb(correlationId,'E',str(e.args),pageInfo,userId,requestId=requestId)
        utils.logger(logger,correlationId,'ERROR','Trace')
        #utils.save_Py_Logs(logger,correlationId)
        raise Exception (e.args[0])        
    else:
        utils.logger(logger,correlationId,'INFO',('\n'+"Data Transformation completed for correlation Id :"+str(correlationId))) 
        utils.save_Py_Logs(logger,correlationId)       