# -*- coding: utf-8 -*-
"""
Created on Fri Jun 21 05:51:35 2019

@author: nitin.john.james
"""
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning)
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
from SSAIutils import utils
import pandas as pd 
import uuid
import time
import numpy as np
import math
import datetime
from pandas import Timestamp


freqDict = {"Yearly": "Y", "Hourly": "H", "Daily": "D","CustomDays": "D", "Weekly": "W",
            "Monthly": "M", "Quarterly": "Q",
             "Half-Year": "6M", "Fortnightly": "2W"}

def main(correlationId,WFId,model,pageInfo,userId):
    
    try:
        steps=5
        logger = utils.logger('Get',correlationId)
        utils.updQdb(correlationId,'P','10',pageInfo,userId,UniId = WFId)  
        utils.logger(logger,correlationId,'INFO',('WF Analysis: '+"Process initiated at : "+ str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S')))+str(None))
        
        dbproconn,dbprocollection = utils.open_dbconn("WhatIfAnalysis")
        data_json = dbprocollection.find({"CorrelationId" :correlationId,"WFId": WFId})  
        dbproconn.close()
        
        steps = int(data_json[0].get('Steps'))
#        Features = data_json[0].get('Features') 
#        for key, value in Features.items():
#            if value.get('Name') == "Steps":
#                steps = int(value.get('Value'))
        dbconn,dbcollection = utils.open_dbconn('SSAI_RecommendedTrainedModels')
        get_version_record = dbcollection.find({"CorrelationId" :correlationId,"modelName":model}) 
        version_list=[]   
        for i in range(0,get_version_record.count()):
                     version_list.append(get_version_record[i].get('Version'))
        
        if max(version_list)!=0:
            version=max(version_list)
        else:
            version=0   
        dbconn.close()                  
        MLDL_Model,_,ProblemType,traincols = utils.get_pickle_file(correlationId,model,'MLDL_Model',version=version)
        utils.updQdb(correlationId,'P','20',pageInfo,userId,UniId = WFId)
        
        

       
        #data_json[0]["RangeTime"]=pd.to_datetime(data_json[0]["RangeTime"].astype(str), format='%d/%m/%Y %H:%M:%S')
        
        dbconn,dbcollection = utils.open_dbconn("SSAI_RecommendedTrainedModels")
        
        data_json = dbcollection.find({"CorrelationId" :correlationId})
        starttime = datetime.datetime.strptime(data_json[0]["RangeTime"][-1],'%d/%m/%Y %H:%M:%S')
        
        
        freq = freqDict[model.split('_')[1]]
        if model.split('_')[1] == "CustomDays":
            _freq,agg,value = utils.getTimeSeriesParams(correlationId)
            freq = str(value)+freq    
        RangeTime = pd.date_range(starttime, periods=steps+1,freq=freq).strftime('%d/%m/%Y %H:%M:%S').astype('str').tolist()[1:]
        if model.split('_')[0] in ["ARIMA"]:
            forecasted = MLDL_Model.forecast(steps)[0].tolist()
        elif model.split('_')[0] in ["Prophet"]:
            future = MLDL_Model.make_future_dataframe(periods = steps,freq=freq)
            forecastdf = MLDL_Model.predict(future)
            forecast = forecastdf['yhat']
            forecasted = forecast.tolist()[:steps]
        else:
            forecasted = MLDL_Model.forecast(steps).tolist()
        
        dbconn, dbcollection = utils.open_dbconn("WF_TestResults")
        utils.updQdb(correlationId,'P','60',pageInfo,userId,UniId = WFId)  
        Id1 = str(uuid.uuid4())   
        dbcollection.insert_many([{"_id" : Id1,  
                                    "CorrelationId":correlationId,
                                    "WFId": WFId,
                                    "ScenarioName":"",
                                    "Temp" :"False",                                    
                                    "Model":model,
                                    "Forecast":forecasted,
                                    "RangeTime":RangeTime,
                                    "Frequency":model.split('_')[1],
                                    "ProblemType": ProblemType,                                   
                                    "CreatedBy":userId,
                                    "CreatedOn":str(datetime.datetime.now())
                                    }])
        time.sleep(1)
        dbconn.close()
        
        utils.updQdb(correlationId,'C','100',pageInfo,userId,UniId = WFId)  
        utils.logger(logger,correlationId,'INFO',('WFAnalysis completed sucessfully at '+ str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))+' WFId :'+ str(WFId)),str(None))        
    except Exception as e:
#        dbproconn.close()
        utils.updQdb(correlationId,'E',str(e.args),pageInfo,userId,UniId = WFId)
        utils.logger(logger,correlationId,'ERROR','Trace',str(None))
        utils.save_Py_Logs(logger,correlationId)
    else:
        utils.logger(logger,correlationId,'INFO',('WF Analysis : '+"Process completed successfully at"+ str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        utils.save_Py_Logs(logger,correlationId)   
    
    
