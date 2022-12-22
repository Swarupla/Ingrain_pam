# -*- coding: utf-8 -*-
"""
Created on Tue Jun 18 18:53:47 2019

@author: nitin.john.james
"""
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
warnings.simplefilter(action='ignore', category=FutureWarning)
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
import threading
import multiprocessing
from queue import Queue
import pandas as pd
import numpy as np
#from pmdarima.arima import auto_arima
from sklearn.model_selection import train_test_split
from sklearn.metrics import mean_squared_error
from statsmodels.tsa.arima_model import ARIMA
from statsmodels.tsa.statespace.sarimax import SARIMAX
from SSAIutils import utils
import time
from evaluation import regression_evaluation
from statsmodels.tsa.arima_model import ARIMAResults
from SSAIutils import EncryptData
from scipy import signal
from datetime import datetime
from pandas import Timestamp
import json

freqDict = {"Yearly": 1, "Hourly": 24, "Daily": 7, "Weekly": 52,
            "Monthly": 12, "Quarterly": 4,
             "Half-Year": 6, "Fortnightly": 2}


def evaluate_arima_model(history,test):
    best_score, best_cfg = float("inf"), None
    predictions = list()
    for p in range(0,2):
        for d in range(0,2):
            for q in range(0,2):
                order = (p,d,q)
                try:
                    model = ARIMA(history, order=order)
                    model_fit = model.fit(disp=0)
                    predictions = model_fit.forecast(len(test))
                    #sprint(test,predictions[0])
                    error = mean_squared_error(test, predictions[0])
                    #print (order,error)
                    if best_score > error:
                        best_score = error
                        best_cfg = order
                except np.linalg.LinAlgError:
                    error_encounterd = 'LinAlgError'                    
                except ValueError:
                    error_encounterd = 'ValueError'
    history.extend(test)                
    return (ARIMA(history, order=best_cfg).fit(),{"order":best_cfg})                         

print_lock = threading.Lock()
  
def crawl(history,test,qu,results):        
        while not qu.empty():
            #print ("Queue length",qu.qsize())
            cfg = qu.get()     
            try:
                with print_lock:        
                    results[cfg] = thread_SARIMAX(history,test,cfg[1].split('_')[0],cfg[1].split('_')[1],cfg[1].split('_')[2])
            except Exception:                
                #print ("Error",cfg)
                results[cfg] = float("inf")                
            qu.task_done()  
        return True     


     
def thread_SARIMAX(history,test,o,so,tr):
        o = eval(o)
        so = eval(so)
        if tr=="None":
            tr=None
        predictions = list()
        model = SARIMAX(history, order=o, seasonal_order=so,trend = tr ,enforce_stationarity=False,enforce_invertibility=False)
        model_fit = model.fit(disp=0)
        predictions = model_fit.forecast(len(test))
        #sprint(test,predictions[0])
        error = mean_squared_error(test, predictions)
        #results[str(o)+"_"+str(so)+"_"+str(tr)]=error
        #print (str(o)+"_"+str(so)+"_"+str(tr),error)
        return error
    

def evaluate_sarima_model(history,test,m):
    #print (m)
    best_score, best_cfg,best_seasonalcfg,trend = float("inf"), None, None, None
    qu = Queue(maxsize=0)
    num_threads = int(multiprocessing.cpu_count()/2)
    results = {}

    threads=[]  
    indx = 0        
    for p in range(0,2):
        for d in range(0,2):
            for q in range(0,2):
                for sp in range(0,2):
                    for sd in range(0,2):
                        for sq in range(0,2):
                            for t in [None]:
                            #for t in [None,'n','c','t','ct']:
                                try:
                                    order = (p,d,q)
                                    seasonalorder = (sp,sd,sq,m)                                    
                                    results[str(order)+"_"+str(seasonalorder)+"_"+str(t)]=best_score
                                    qu.put((indx,str(order)+"_"+str(seasonalorder)+"_"+str(t)))                                    
                                    indx += 1                                    
                                except Exception as e:
                                    error_encounterd = str(e.args[0])
                                    #print (order, seasonalorder,trend)
    for i in range(num_threads):
        worker = threading.Thread(target=crawl,args=(history,test,qu,results))
        worker.start()
        threads.append(worker)
    qu.join()    
    for t in threads:
        t.join()
        
    for cfg,error in results.items(): 
        #print (cfg)                             
        if best_score > error:
                best_score = error
                best_cfg = eval(cfg[1].split('_')[0])
                best_seasonalcfg = eval(cfg[1].split('_')[1])
                trend = cfg[1].split('_')[2]  if cfg[1].split('_')[2]!="None" else None                              
    #print (best_cfg,best_seasonalcfg,trend) 
    history.extend(test)	
    return (SARIMAX(history, order=best_cfg,seasonal_order=best_seasonalcfg,trend = trend,enforce_stationarity=False,enforce_invertibility=False).fit(),{"order":best_cfg,"seasonal_order":best_seasonalcfg,"trend":trend,"enforce_stationarity":False,"enforce_invertibility":False})
               

def main(correlationId,modelName,pageInfo,userId,requestId,problemtype,seasonality=True,version=None):
    
    logger = utils.logger('Get',correlationId)    
    start = time.time()    
    try:
        
                
        test_size=0.2
        
        # Pulling data
        #utils.insQdb(correlationId,'P','10',pageInfo,userId,modelName = modelName,problemType='TimeSeries')
        data = utils.data_timeseries(correlationId,'DE_PreProcessedData')
        #utils.logger(logger,correlationId,'INFO',("Modelname "+modelName+" Data Fetched at : "+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        utils.updQdb(correlationId, 'P', '20', pageInfo, userId,requestId=requestId,modelName=modelName, problemType=problemtype, UniId=None,retrycount=3,Incremental=False)
        freq,agg,_ = utils.getTimeSeriesParams(correlationId)
        DateCol = utils.getDateCol(correlationId)
        targetvariable = None
        seasonal=seasonality 
        anomaly = {}
        #print (freq,data)
        for i,selectedFreq  in enumerate(freq):
            #m = freqDict[selectedFreq]      
            #m=1
            
           # forecastPeriod = int(freq[selectedFreq])
            df = data[selectedFreq]
            datafrq = df.copy()
            df.set_index(df[DateCol],drop=True,inplace=True)
            df.index = pd.to_datetime(df.index)
            df.sort_index(inplace=True)
            
            _modelName = modelName+"_"+selectedFreq

            try:
                logR = np.diff(np.log(df[df.columns.difference([DateCol])[0]].tolist()))
                periodgram = signal.periodogram(logR)
                maxindx = np.where(periodgram[1] == np.amax(periodgram[1]))[0][0]
                m = int(round(1/periodgram[0][maxindx]))            
                if m>26:
                    m=1
            except Exception:
                m=1
            '''
            train and test split the dataset for vaidation 
            '''
            train, test = train_test_split(df[df.columns.difference([DateCol])],test_size=test_size,shuffle=False)    
            #utils.logger(logger,correlationId,'INFO',('Modelname '+_modelName+" Test Train split at : "+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
            '''
            create auto_arima function based in the range of orders given as function parameters
            '''
            if seasonal:
                model,cfg = evaluate_sarima_model(train[train.columns.difference([DateCol])].values.flatten().tolist(),test[test.columns.difference([DateCol])].astype(float).values.flatten().tolist(),m)

                forecast = model.forecast(len(test))
            else:
                model,cfg = evaluate_arima_model(train[train.columns.difference([DateCol])].astype(float).values.flatten().tolist(),test[test.columns.difference([DateCol])].astype(float).values.flatten().tolist())

                forecast = model.forecast(len(test))[0]
            
            '''
            Testing the model forecast accuracy usng Mean Square Error
            '''
        
           
            #forecast_df = pd.DataFrame([],index = test.index,column3s=["Prediction","Actual"])
            #forecast_df["Prediction"]=forecast
            #forecast_df["Actual"]=test[test.columns.difference(['DateCol'])].tolist()
            utils.updQdb(correlationId, 'P', '40', pageInfo, userId,requestId=requestId,modelName=modelName, problemType=problemtype, UniId=None,retrycount=3,Incremental=False)
            lastDataRecorded = test.index.strftime('%Y-%m-%d %H:%M:%S').astype('str').tolist()[-1]
            RangeTime = test.index.strftime('%d/%m/%Y %H:%M:%S').astype('str').tolist()
         
            r2ScoreVal,rmsVal,maeVal,mseVal = regression_evaluation.evaluate_reg(test[test.columns.difference([DateCol])].astype(float).values.flatten().tolist(), forecast.tolist(), multioutput=None,timeseries=True)
            #print (r2ScoreVal,rmsVal,maeVal,mseVal)
            '''
            save the forecasted value with the trained model
            '''
            end = time.time()            
            utils.save_file((model,cfg),_modelName,'TimeSeries',correlationId,pageInfo,userId,list(train.columns),'MLDL_Model',version=version)
            xlabelname = DateCol
            ylabelname = test.columns.difference([DateCol])[0]
            
            steps = 1
            forecastedvalue= model.get_forecast(steps)
            forecasted =[max(0.0,round(float(each),2)) for each in forecastedvalue.predicted_mean]
            upperbound = [each[1] for each in forecastedvalue.conf_int()]
            lowerbound = [each[0] for each in forecastedvalue.conf_int()]
            threshold= 1.5*(np.array(upperbound) - np.array(lowerbound))
            tested = test.values.reshape(-1, 1)
            absolutevalue =abs(forecasted - tested)        
            anomalyvalues = absolutevalue > threshold
            anomalyvalues = np.concatenate(anomalyvalues).tolist()
            datafrq.index = np.arange(1, len(datafrq) + 1)
            datafrq = datafrq.to_json()
            datafrq = EncryptData.EncryptIt(json.dumps(datafrq))
            #test = np.concatenate(test).tolist()
            if isinstance(tested, pd.DataFrame):
                tested.index = np.arange(1, len(tested) + 1)
                tested = tested.to_json()
                tested = EncryptData.EncryptIt(json.dumps(tested))
            else:
                tested = np.concatenate(tested).tolist()
            anomaly.update({selectedFreq:{"anomaly":anomalyvalues,"Forecast":str(forecast),"actualdata":tested,"input":datafrq}})
            utils.insert_EvalMetrics_FI_T(correlationId,_modelName,'TimeSeries',r2ScoreVal, rmsVal, maeVal, mseVal,end-start,test[test.columns.difference([DateCol])].astype(float).values.flatten().tolist(),forecast.tolist(),RangeTime,selectedFreq,lastDataRecorded,xlabelname,ylabelname,"forecast",pageInfo,userId,version=version,anomalyvalues = anomaly)
            '''
            update progress in queue table
            '''
            updatevalue = int(10 + 89*(i+1)/len(freq))
            #utils.updQdb(correlationId,'P',str(updatevalue),pageInfo,userId,modelName = modelName,problemType='TimeSeries')
            utils.updQdb(correlationId, 'C', '100', pageInfo, userId,requestId=requestId,modelName=modelName, problemType=problemtype, UniId=None,retrycount=3,Incremental=False)
            #utils.logger(logger,correlationId,'INFO',('Modelname '+_modelName+" Model Created and Saved at : "+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
            #model.fit(df)
            #forecast = model.predict(n_periods=forecastPeriod)
    except Exception as e:
        utils.updQdb(correlationId,'E',str(e.args),pageInfo,userId,requestId=requestId,modelName = modelName,problemType=problemtype)
        utils.logger(logger,correlationId,'ERROR','Trace')
        utils.save_Py_Logs(logger,correlationId)
    else:
        utils.logger(logger,correlationId,'INFO',('\n'+"Model Training completed for correlation Id :"+str(correlationId))) 
        utils.save_Py_Logs(logger,correlationId)      
        
        
       
#main("93a07308-3540-4453-b024-407500b75f9c","ARIMA",1,1,seasonality=False) 
