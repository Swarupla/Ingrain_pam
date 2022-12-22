# -*- coding: utf-8 -*-
"""
Created on Wed Jun 19 13:44:29 2019

@author: nitin.john.james
"""

# -*- coding: utf-8 -*-
"""
Created on Wed Jun 19 12:20:48 2019

@author: nitin.john.james
"""
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
warnings.simplefilter(action='ignore', category=FutureWarning)
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
from statsmodels.tsa.holtwinters import ExponentialSmoothing
import pandas as pd
import numpy as np
from sklearn.model_selection import train_test_split
from SSAIutils import utils
import time
from evaluation import regression_evaluation

from scipy import signal

from datetime import datetime
from pandas import Timestamp

freqDict = {"Yearly": 1, "Hourly": 24, "Daily": 7, "Weekly": 52,
            "Monthly": 12, "Quarterly": 4,
             "Half-Year": 6, "Fortnightly": 2}


def main(correlationId,modelName,pageInfo,userId,version=None):
    logger = utils.logger('Get',correlationId)   
    start = time.time() 
    try:
    # Pulling data
        utils.updQdb(correlationId,'P','10',pageInfo,userId,modelName = modelName,problemType='TimeSeries')
        data = utils.data_timeseries(correlationId,'DE_PreProcessedData')
        DateCol = utils.getDateCol(correlationId)
        utils.logger(logger,correlationId,'INFO',('Modelname '+modelName+" Data Fetched at : "+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        
        test_size = 0.2
        
        freq,_ ,_= utils.getTimeSeriesParams(correlationId)
        for i,selectedFreq  in enumerate(freq):
            #print(i,selectedFreq )
            #m = freqDict[selectedFreq] 
            #m=1
            df = data[selectedFreq]
            df.set_index(df[DateCol],drop=True,inplace=True)
            df.index = pd.to_datetime(df.index)
            df.sort_index(inplace=True)

            try:
                logR = np.diff(np.log(df[df.columns.difference([DateCol])[0]].tolist()))
                periodgram = signal.periodogram(logR)
                maxindx = np.where(periodgram[1] == np.amax(periodgram[1]))[0][0]
                m = int(round(1/periodgram[0][maxindx]))
                
                if m <2 or m>26 :
                    m=2
            except Exception:
                m=2
            
           # val = df.columns.difference([DateCol])[0]
           # df.loc[df[val]<=0,val]=0.1
            _modelName = modelName+"_"+selectedFreq
                    
            '''
            train and test split the dataset for vaidation 
            '''
            train, test = train_test_split(df,test_size=test_size,shuffle=False)    
            utils.logger(logger,correlationId,'INFO',('Modelname '+_modelName+" Test Train split at : "+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))))
            
            model_fit={}            
            
            model_fit["fit0"] = ExponentialSmoothing(np.asarray(train[train.columns.difference([DateCol])]), seasonal_periods=m, trend='add', seasonal='add').fit(use_boxcox=False)
            model_fit["pred0"]= model_fit["fit0"].forecast(len(test))
            model_fit["conf0"]= {"seasonal_periods":m, "trend":'add', "seasonal":'add',"damped":False}
            
            model_fit["fit1"] = ExponentialSmoothing(np.asarray(train[train.columns.difference([DateCol])]), seasonal_periods=m, trend='add', seasonal='add').fit(use_boxcox=False,smoothing_level=0.8, smoothing_slope=0.2)
            model_fit["pred1"]= model_fit["fit1"].forecast(len(test))
            model_fit["conf1"]= {"seasonal_periods":m, "trend":'add', "seasonal":'add',"damped":False,"smoothing_level":0.8, "smoothing_slope":0.2}
            
            model_fit["fit2"] = ExponentialSmoothing(np.asarray(train[train.columns.difference([DateCol])]), seasonal_periods=m, trend='add', seasonal='add', damped=True).fit(use_boxcox=False)
            model_fit["pred2"]= model_fit["fit2"].forecast(len(test))
            model_fit["conf2"]= {"seasonal_periods":m, "trend":'add', "seasonal":'add',"damped":True}
            
            model_fit["fit3"] = ExponentialSmoothing(np.asarray(train[train.columns.difference([DateCol])]), seasonal_periods=m, trend='add', seasonal='add', damped=True).fit(use_boxcox=False,smoothing_level=0.8, smoothing_slope=0.2)    
            model_fit["pred3"]= model_fit["fit3"].forecast(len(test))
            model_fit["conf3"]= {"seasonal_periods":m, "trend":'add', "seasonal":'add',"damped":True,"smoothing_level":0.8, "smoothing_slope":0.2}
            '''
            Testing the model forecast accuracy usng Mean Square Error
            '''
        
            
            scores = [] 
            #print (model_fit)
            for val in range(4):
                if  np.all(np.invert(np.isnan(model_fit["pred"+str(val)]))):
                     _,_,_,mseVal = regression_evaluation.evaluate_reg(test[test.columns.difference([DateCol])].astype(float).values.flatten().tolist(), model_fit["pred"+str(val)].tolist(), multioutput=None)
                     scores.append(mseVal["error_rate"])
                else:
                    scores.append(99999999)
            selectedModel = model_fit["fit"+str(scores.index(min(scores)))]
            selectedConf = model_fit["conf"+str(scores.index(min(scores)))]
            forecastedValues = model_fit["pred"+str(scores.index(min(scores)))].tolist()
            if "smoothing_level" not in selectedConf.keys():
                selectedModel = ExponentialSmoothing(np.asarray(df[df.columns.difference([DateCol])]),seasonal_periods=selectedConf["seasonal_periods"], trend=selectedConf["trend"], seasonal=selectedConf["seasonal"],damped = selectedConf["damped"]).fit(use_boxcox=False)
            else:
                selectedModel= ExponentialSmoothing(np.asarray(df[df.columns.difference([DateCol])]),seasonal_periods=selectedConf["seasonal_periods"], trend=selectedConf["trend"], seasonal=selectedConf["seasonal"],damped = selectedConf["damped"]).fit(use_boxcox=False,smoothing_level=selectedConf["smoothing_level"], smoothing_slope=selectedConf["smoothing_slope"])     
            #forecast_df = pd.DataFrame([],index = test.index,column3s=["Prediction","Actual"])
            #forecast_df["Prediction"]=forecast
            #forecast_df["Actual"]=test[test.columns.difference([DateCol])].tolist()
            lastDataRecorded = test.index.strftime('%Y-%m-%d %H:%M:%S').astype('str').tolist()[-1]
            RangeTime = test.index.strftime('%d/%m/%Y %H:%M:%S').astype('str').tolist()
         
            r2ScoreVal,rmsVal,maeVal,mseVal = regression_evaluation.evaluate_reg(test[test.columns.difference([DateCol])].astype(float).values.flatten().tolist(), forecastedValues, multioutput=None,timeseries=True)
            
            '''
            save the forecasted value with the trained model
            '''
            end = time.time()
            utils.save_file((selectedModel,selectedConf),_modelName,'TimeSeries',correlationId,pageInfo,userId,list(train.columns),'MLDL_Model',version=version)
            xlabelname = DateCol
            ylabelname = test.columns.difference([DateCol])[0]
            utils.insert_EvalMetrics_FI_T(correlationId,_modelName,'TimeSeries',r2ScoreVal, rmsVal, maeVal, mseVal,end-start,test[test.columns.difference([DateCol])].astype(float).values.flatten().tolist(),forecastedValues,RangeTime,selectedFreq,lastDataRecorded,xlabelname,ylabelname,"forecast",pageInfo,userId,version=version)
            '''
            update progress in queue table
            '''
            updatevalue = int(10 + 89*(i+1)/len(freq))
            utils.updQdb(correlationId,'P',str(updatevalue),pageInfo,userId,modelName = modelName,problemType='TimeSeries')
            utils.logger(logger,correlationId,'INFO',('Modelname '+_modelName+" Model Created and Saved at : "+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        utils.updQdb(correlationId,'C','100',pageInfo,userId,modelName = modelName,problemType='TimeSeries')        
    except Exception as e:
            utils.updQdb(correlationId,'E',str(e.args),pageInfo,userId,modelName = modelName,problemType='TimeSeries')
            utils.logger(logger,correlationId,'ERROR','Trace',str(None))
            utils.save_Py_Logs(logger,correlationId)
    else:
            utils.logger(logger,correlationId,'INFO',('ModelName '+modelName+" Training Model completed at : "+ str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None)) 
            utils.save_Py_Logs(logger,correlationId)
#main("429a9871-26ae-48f1-a0e3-5322810e83f8","RecommendedAI_HWES",1,1)  
