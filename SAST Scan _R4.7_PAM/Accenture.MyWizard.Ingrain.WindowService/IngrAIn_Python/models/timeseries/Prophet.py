# -*- coding: utf-8 -*-
"""
Created on Tue Jun 18 18:53:47 2019

@author: nitin.john.james
"""
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
import pandas as pd
from fbprophet import Prophet
import logging,os

from SSAIutils import utils
import time
from evaluation import regression_evaluation
from sklearn.model_selection import train_test_split
logging.getLogger('fbprophet').setLevel(logging.WARNING)
from datetime import datetime
from pandas import Timestamp

freqDict = {"Yearly": "Y", "Hourly": "H", "Daily": "D", "CustomDays": "D", "Weekly": "W",
            "Monthly": "M", "Quarterly": "Q",
             "Half-Year": "6M", "Fortnightly": "2W"}
class suppress_stdout_stderr(object):
    '''
    A context manager for doing a "deep suppression" of stdout and stderr in
    Python, i.e. will suppress all print, even if the print originates in a
    compiled C/Fortran sub-function.
       This will not suppress raised exceptions, since exceptions are printed
    to stderr just before a script exits, and after the context manager has
    exited (at least, I think that is why it lets exceptions through).

    '''
    def __init__(self):
        # Open a pair of null files
        self.null_fds = [os.open(os.devnull, os.O_RDWR) for x in range(2)]
        # Save the actual stdout (1) and stderr (2) file descriptors.
        self.save_fds = [os.dup(1), os.dup(2)]

    def __enter__(self):
        # Assign the null pointers to stdout and stderr.
        os.dup2(self.null_fds[0], 1)
        os.dup2(self.null_fds[1], 2)

    def __exit__(self, *_):
        # Re-assign the real stdout/stderr back to (1) and (2)
        os.dup2(self.save_fds[0], 1)
        os.dup2(self.save_fds[1], 2)
        # Close the null files
        for fd in self.null_fds + self.save_fds:
            os.close(fd)


			 
def main(correlationId,modelName,pageInfo,userId,version=None):
    
    logger = utils.logger('Get',correlationId)   
    start = time.time()    
    try:
        
                
        test_size=0.2
        
        # Pulling data
        utils.insQdb(correlationId,'P','10',pageInfo,userId,modelName = modelName,problemType='TimeSeries')
        data = utils.data_timeseries(correlationId,'DE_PreProcessedData')
        utils.logger(logger,correlationId,'INFO',('Modelname '+modelName+" Data Fetched at : "+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        
        freq,_,value = utils.getTimeSeriesParams(correlationId)
        DateCol = utils.getDateCol(correlationId)
        #print (freq,data)
        for i,selectedFreq  in enumerate(freq):
            #m = freqDict[selectedFreq]      
            #m=1
            
           # forecastPeriod = int(freq[selectedFreq])
            df = data[selectedFreq]
            df.set_index(df[DateCol],drop=True,inplace=True)
            df.index = pd.to_datetime(df.index)
            df.index = df.index.tz_localize(None)
            df.sort_index(inplace=True)
            
            _modelName = modelName+"_"+selectedFreq
			
            _freq = freqDict[selectedFreq]
            if selectedFreq == "CustomDays":
               _freq = str(value)+_freq
			   
            '''
            train and test split the dataset for vaidation 
            '''
            train, test = train_test_split(df[df.columns.difference([DateCol])],test_size=test_size,shuffle=False)    
            utils.logger(logger,correlationId,'INFO',('Modelname '+_modelName+" Test Train split at : "+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))

            model=Prophet(interval_width=0.95) 
            train.rename(columns = {DateCol:"ds",df.columns.difference([DateCol])[0]:"y"},inplace=True)
            train["ds"] = train.index
            #train.to_csv("prophet.csv")
            with suppress_stdout_stderr():

               model.fit(train)
            cfg = None
            
            future = model.make_future_dataframe(periods = len(test),freq=_freq)
            #future.tail()
            #print (future.ds)
            #print (df.index)
            forecastdf = model.predict(future)
            #forecastdf[['ds', 'yhat', 'yhat_lower', 'yhat_upper']].tail()
            forecast = forecastdf['yhat']
            #print (len(forecast.tolist()[:len(test)]))
            #print (test.shape[0])			
            
            '''
            Testing the model forecast accuracy usng Mean Square Error
            '''
        
           
            #forecast_df = pd.DataFrame([],index = test.index,column3s=["Prediction","Actual"])
            #forecast_df["Prediction"]=forecast
            #forecast_df["Actual"]=test[test.columns.difference(['DateCol'])].tolist()
            lastDataRecorded = test.index.strftime('%Y-%m-%d %H:%M:%S').astype('str').tolist()[-1]
            RangeTime = test.index.strftime('%d/%m/%Y %H:%M:%S').astype('str').tolist()
         
            r2ScoreVal,rmsVal,maeVal,mseVal = regression_evaluation.evaluate_reg(test[test.columns.difference([DateCol])].astype(float).values.flatten().tolist(), forecast.tolist()[:len(test)], multioutput=None,timeseries=True)
            #print (r2ScoreVal,rmsVal,maeVal,mseVal)
            '''
            save the forecasted value with the trained model
            '''
            end = time.time()            
            utils.save_file((model,cfg),_modelName,'TimeSeries',correlationId,pageInfo,userId,list(train.columns),'MLDL_Model',version=version)
            xlabelname = DateCol
            ylabelname = test.columns.difference([DateCol])[0]
            utils.insert_EvalMetrics_FI_T(correlationId,_modelName,'TimeSeries',r2ScoreVal, rmsVal, maeVal, mseVal,end-start,test[test.columns.difference([DateCol])].astype(float).values.flatten().tolist(),forecast.tolist()[:len(test)],RangeTime,selectedFreq,lastDataRecorded,xlabelname,ylabelname,"forecast",pageInfo,userId,version=version)
            '''
            update progress in queue table
            '''
            updatevalue = int(10 + 90*(i+1)/len(freq))
            utils.updQdb(correlationId,'P',str(updatevalue),pageInfo,userId,modelName = modelName,problemType='TimeSeries')
            utils.logger(logger,correlationId,'INFO',('Modelname '+_modelName+" Model Created and Saved at : "+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        utils.updQdb(correlationId,'C','100',pageInfo,userId,modelName = modelName,problemType='TimeSeries')        
            #model.fit(df)
            #forecast = model.predict(n_periods=forecastPeriod)
    except Exception as e:
        utils.updQdb(correlationId,'E',str(e.args),pageInfo,userId,modelName = modelName,problemType='TimeSeries')
        utils.logger(logger,correlationId,'ERROR','Trace',str(None))
        utils.save_Py_Logs(logger,correlationId)
    else:
        utils.logger(logger,correlationId,'INFO',('Modelname '+modelName+" Training Model completed at : "+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None)) 
        utils.save_Py_Logs(logger,correlationId)      
        
        
       
#main("93a07308-3540-4453-b024-407500b75f9c","ARIMA",1,1,seasonality=False) 
