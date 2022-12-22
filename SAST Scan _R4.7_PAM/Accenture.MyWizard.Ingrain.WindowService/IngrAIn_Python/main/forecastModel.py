import time
start = time.time()
import psutil
cpu=str(psutil.cpu_percent())
memory=str(psutil.virtual_memory()[2])

import platform
if platform.system() == 'Linux':
    conf_path = '/IngrAIn_Python/main/pythonconfig.ini'
    work_dir = '/IngrAIn_Python'
elif platform.system() == 'Windows':
    conf_path = '\IngrAIn_Python\main\pythonconfig.ini'
    work_dir = '\IngrAIn_Python'
import configparser,os
mainPath =os.getcwd()+work_dir
import sys
sys.path.insert(0,mainPath)

import file_encryptor

config = configparser.RawConfigParser()
configpath = str(os.getcwd()) + str(conf_path)
try:
        config.read(configpath)
except UnicodeDecodeError:
        config = file_encryptor.get_configparser_obj(configpath)
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
from SSAIutils import utils
import pandas as pd
import numpy as np
from pandas import Timestamp
from numpy import nan
from datapreprocessing import aggregation
from datetime import datetime,timedelta
#import statsmodels as sm
import math
import requests
import numpy as np
from datapreprocessing import DataEncoding
from statsmodels.tsa.arima_model import ARIMA
from statsmodels.tsa.statespace.sarimax import SARIMAX
from statsmodels.tsa.holtwinters import SimpleExpSmoothing
from statsmodels.tsa.holtwinters import ExponentialSmoothing
#from fbprophet import Prophet
from SSAIutils import EncryptData #En............................
import base64
import json
end = time.time()

freqDict = {"Yearly": "Y", "Hourly": "H", "Daily": "D","CustomDays": "D","Weekly": "W",
            "Monthly": "M", "Quarterly": "Q",
             "Half-Year": "6M", "Fortnightly": "2W"}

freqRange = {"Yearly": 400, "Hourly": 1, "Daily": 3, "CustomDays":7,"Weekly": 14,
            "Monthly": 60, "Quarterly": 180,
             "Half-Year": 200, "Fortnightly": 28}

def diff_days(day1,day2):
    date_format = "%d-%m-%Y"
    #day1 = datetime.strptime(day1, date_format)
    day2 = datetime.strptime(day2, date_format)
    return (day1 - day2).days

def main_wrapper(correlationId,uniqueId,pageInfo="publishModel"):
    try:
        dbconn,dbcollection = utils.open_dbconn('SSAI_PublishModel')
        data_json = list(dbcollection.find({"CorrelationId":correlationId,"UniqueId":uniqueId}))
        if not data_json:
           raise Exception ("Unable to find data for corresponding correlationId and UniqueId")
        #print(data_json)
            #DataProCollectionId = data_json[0].get('_id')
        DateCol = utils.getDateCol(correlationId)
        EnDeRequired = utils.getEncryptionFlag(correlationId)

        frequency = data_json[0].get("Frequency")
           
        k = 0
        dbconn.close()
        logger = utils.logger('Get',correlationId)

        if len(data_json) == 1:
            dbcollection.update_one({"CorrelationId":correlationId,"UniqueId":uniqueId,"Chunk_number":k},{'$set':{         
                                    'Status' : "P",
                                    'Progress':"0",
                                    'ErrorMessage':""
                                   }}) 
            actual_data = data_json[0].get("ActualData")
            if EnDeRequired:
                t = base64.b64decode(actual_data)
                actual_data = EncryptData.DescryptIt(t)
                #print("After decryption",actual_data)
            Data = actual_data.strip().rstrip()
            #print ("Data is :",Data)
            if isinstance(Data,str):
                try:
                    if Data!="null":
                        Data = eval(Data)
                except SyntaxError:
                    Data = eval(''.join([i if ord(i) < 128 else ' ' for i in Data]))
            #print("Data",Data)
            if Data!="null":
                data = pd.DataFrame(Data)
                data = data.rename(columns=lambda x: x.strip())
               
                if DateCol not in data.columns.tolist():
                    raise Exception( "Date Column specified earlier not found")
                #print(data)
                data.replace(r'^\s*$',np.nan,regex=True,inplace=True)
                data.fillna(0, inplace = True) 
            else:
                data = None
            utils.logger(logger, correlationId, 'INFO', ('main function called at'  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(uniqueId))

            main(data,Data,frequency,k,prediction = False)
   
        else:
            dbcollection.update_one({"CorrelationId":correlationId,"UniqueId":uniqueId,"Chunk_number":k},{'$set':{         
                                    'Status' : "P",
                                    'Progress':"0",
                                    'ErrorMessage':""
                                   }})  
            data = pd.DataFrame()
            for i in range(len(data_json)):
                actual_data = data_json[i].get("ActualData")
                if EnDeRequired:
                    t = base64.b64decode(actual_data)
                    actual_data = EncryptData.DescryptIt(t)
                Data = actual_data.strip().rstrip()
                #print ("Data is :",Data)
                if isinstance(Data,str):
                    try:
                        if Data!="null":
                            Data = eval(Data)
                    except SyntaxError:
                        Data = eval(''.join([i if ord(i) < 128 else ' ' for i in Data]))
          	
                if Data!="null":
                    data = data.append(pd.DataFrame(Data))
                    data = data.sort_values(by = DateCol)
                
                    if DateCol not in data.columns.tolist():
                        raise Exception( "Date Column specified earlier not found")
                #print(data)
                    data.replace(r'^\s*$',np.nan,regex=True,inplace=True)
                    data.fillna(0, inplace = True)

                #print("k:::::::::::::",k)
            utils.logger(logger, correlationId, 'INFO', ('main function called at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))+ ' for the chunk '+ str(k)),str(uniqueId))

            main(data,Data,frequency,k,prediction = True)
    except Exception as e:
        raise Exception(e)
          
    return 

def main(data,Data,frequency,chunk_number,prediction):
    frequency = frequency
    data = data
    Data = Data
    logger = utils.logger('Get',correlationId)        
    DateCol = utils.getDateCol(correlationId)
    EnDeRequired = utils.getEncryptionFlag(correlationId)
    #print("CorrelationId::", correlationId, "\nUniqueId::", uniqueId)
    dbconn,dbcollection = utils.open_dbconn("PS_BusinessProblem")
    data_json = dbcollection.find({"CorrelationId" :correlationId})    
    f={}
    fdict = {int(k):v for k,v in data_json[0]["TimeSeries"]["Frequency"].items()}
    for each in fdict:
        if fdict[each]["Steps"]:
            f[fdict[each]["Name"]]=fdict[each]["Steps"]
            if fdict[each]["Name"]=="CustomDays":
                value = fdict[each]["value"]
    target_variable = data_json[0].get('TargetColumn')    
    target_variable = target_variable.strip()
    agg = data_json[0]["TimeSeries"]["Aggregation"]  
    if Data!="null":
      if target_variable not in data.columns.tolist():
        raise Exception( "Target Column specified earlier not found")

    if Data!="null":
        data = data[[DateCol,target_variable]]
    
    dbconn,dbcollection = utils.open_dbconn('SSAI_DeployedModels')
    data_json = list(dbcollection.find({"CorrelationId":correlationId,"Frequency":frequency}))
    IsMutipleApp = (data_json[0].get("IsMutipleApp",False)) ####changes for releaseplanner trained from SPA
    if IsMutipleApp:
        dbconn_rp,dbcollection_rp = utils.open_dbconn('SSAI_PublishModel')
        data_json_rp = list(dbcollection_rp.find({"CorrelationId":correlationId,"UniqueId":uniqueId})) 
        AppID = data_json_rp[0].get("AppID")
        UsecaseId = data_json_rp[0].get('TempalteUseCaseId','None')
        dbconn_rp.close()
    else:    
        AppID = data_json[0].get("AppId")
        UsecaseId = data_json[0].get('TemplateUsecaseId','None')
    Ambulance = False
    if UsecaseId in ["fa52b2ab-6d7f-4a97-b834-78af04791ddf","169881ad-dc85-4bf8-bc67-7b1212836a97","be0c67a1-4320-461e-9aff-06a545824a32",
                     "6761146a-0eef-4b39-8dd8-33c786e4fb86","8d3772f2-f19b-4403-840d-2fb230ac630f","668bb66a-86c6-46e6-9f98-c0bc9b3e4eb2"]:
         Ambulance = True
    data_t = None
    if AppID in ["595fa642-5d24-4082-bb4d-99b8df742013","a3798931-4028-4f72-8bcd-8bb368cc71a9","9fe508f7-64bc-4f58-899b-78f349707efa"]:
         customurl_token,status_code=utils.CustomAuth(AppID)
         invokeIngestData,_,_ = utils.getRequestParams(correlationId,None)
         invokeIngestData=eval(invokeIngestData)
         if status_code!=200:
             raise Exception ("Unable to find model for corresponding correlationId")
              
         else:
              startdate = invokeIngestData['Customdetails']['InputParameters'].get("EndDate")
              enddate = datetime.now().strftime("%Y-%m-%d")
              enddate = (datetime.strptime(enddate, '%Y-%m-%d')+timedelta(days=1)).strftime('%Y-%m-%d')
              if (datetime.strptime(enddate, "%Y-%m-%d")-datetime.strptime(startdate, "%Y-%m-%d")).days > 0:                  
                  TotalRecordCount = 0
                  TotalPageCount =  1
                  PageNumber = 1
                  BatchSize = invokeIngestData['Customdetails']['InputParameters'].get("BatchSize")
                  data = None                  
                  while PageNumber <=TotalPageCount:
                      jsondata = {
                                "ClientUID" : invokeIngestData["ClientUID"], 
                                "DeliveryConstructUId" : [invokeIngestData["DeliveryConstructUId"]], 
                                "StartDate" : startdate, 
                                "EndDate" : enddate, 
                                "TotalRecordCount" : TotalRecordCount,
                                "PageNumber":PageNumber,
                                "BatchSize":int(BatchSize)
                              }
                      if Ambulance:                       
                              jsondata["IterationUId"]=eval(invokeIngestData['Customdetails']['InputParameters'].get("IterationUId"))
                      if UsecaseId in ["64a6c5be-0ecb-474e-b970-06c960d6f7b7","5cab6ea1-8af4-4f74-8359-e053629d2b98","68bf25f3-6df8-4e14-98fa-34918d2eeff1"]:   #SPA
                          jsondata["IsTeamLevelData"] = "0"
                      elif UsecaseId == 'f0320924-2ee3-4398-ad7c-8bc172abd78d':    #timeseries
                          jsondata["IsTeamLevelData"] = "1"
                          Teamlevel = invokeIngestData['Customdetails']['InputParameters'].get("TeamAreaUId",None)
                          if Teamlevel != "null" and Teamlevel != None:
                              jsondata["TeamAreaUId"]=[Teamlevel]

                      utils.logger(logger, correlationId, 'INFO', ('Hadoop API called at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(uniqueId))

                      result=requests.post(invokeIngestData.get('Customdetails').get('AppUrl'),data=json.dumps(jsondata),headers={'Content-Type': 'application/json',
                                                         'Authorization': 'Bearer {}'.format(customurl_token),'AppServiceUId':config['HadoopApi']['AppServiceUId']})
                      utils.logger(logger, correlationId, 'INFO', ('Response from Hadoop API received at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(uniqueId))

                      #print (invokeIngestData.get('Customdetails').get('AppUrl'),jsondata,{'Content-Type': 'application/json',
                                                       #  'Authorization': 'Bearer {}'.format(customurl_token)})
                      #print (result.json())
                      if result.status_code!=200:
                          raise Exception ("The Status Code after hitting post token generation is "+str(result.status_code))
                          break
                      else:
                          data_j=result.json()
                          #print (data_json)
                          if len(data_j["Client"]) <= 0:
                                  #MultiSourceDfs['Custom'] = {"Custom":"No Data Available for Selection"}
                                  data=pd.DataFrame()
                                  break
                          else:
                              if PageNumber == 1:                              
                                  data=pd.DataFrame(data_j['Client'][0]["Items"])
                                  TotalRecordCount = data_j["TotalRecordCount"]
                                  TotalPageCount = data_j["TotalPageCount"]
                              else:
                                  temp_data = pd.DataFrame(data_j['Client'][0]["Items"])
                                  data = data.append(temp_data,ignore_index=True)                              
                              PageNumber = PageNumber + 1
                  #print ("#############hereee",data)
                  if not data.empty:
                      if not Ambulance:
                          data = data.sort_values(by="StartDate")
                          aggDays= (datetime.strptime(data['EndDate'].iloc[-1], "%Y-%m-%d %H:%M:%S") - datetime.strptime(data['StartDate'].iloc[-1], "%Y-%m-%d %H:%M:%S")).days
                          #print (aggDays)
                      else:
                          data = data.sort_values(by="StartOn")
                          aggDays= (datetime.strptime(data['EndOn'].iloc[-1], "%Y-%m-%d %H:%M:%S") - datetime.strptime(data['StartOn'].iloc[-1], "%Y-%m-%d %H:%M:%S")).days
                      data_t = {"CustomDays":data[[DateCol,target_variable]]}
                      data_t["CustomDays"].set_index(DateCol,drop=True,inplace=True)
                      data_t["CustomDays"].index = pd.to_datetime(data_t["CustomDays"].index)
                      data_t["CustomDays"].sort_index(inplace=True)
                      freqRange["CustomDays"] = 61
                  else:
                      aggDays = value
                      data=None
              else:
                  aggDays = value
                  data=None
    if data_json :
        #print ("hereerer",data_json)
        selectedModel = data_json[0].get("ModelVersion")   
        frequency = selectedModel.split('_')[1]
        if Data!="null":
            dbproconn,dbprocollection = utils.open_dbconn("DE_DataProcessing")
            data_json = dbprocollection.find({"CorrelationId" :correlationId})
            utils.logger(logger, correlationId, 'INFO', ('Encoding started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(uniqueId))
            
                    #Encoding                    
            Data_to_Encode=data_json[0].get('DataEncoding')    
            OHEcols = []
            LEcols = []
            for keys,values in Data_to_Encode.items():                
                if values.get('encoding') == 'One Hot Encoding' and (values.get('ChangeRequest') == 'True' and values.get('PChangeRequest') != 'True') and (keys != 'ChangeRequest' and keys!='PChangeRequest'):
                    OHEcols.append(keys)
                elif values.get('encoding') == 'Label Encoding' and (values.get('ChangeRequest') == 'True' and values.get('PChangeRequest') != 'True') and (keys != 'ChangeRequest' and keys!='PChangeRequest'):
                    LEcols.append(keys)
            if OHEcols:
                ohem,_,enc_cols,_ = utils.get_pickle_file(correlationId,FileType='OHE')
                data,data_ohe = DataEncoding.one_hot_dec(ohem,data,OHEcols,enc_cols)       
                
            LETarget = 'None'
            if target_variable in LEcols:
               # LEcols.remove(target_variable)  
                LETarget = True
                
            if LEcols:
                lencm,_,_,_ = utils.get_pickle_file(correlationId,FileType='LE')
                if LETarget==True:
                    if len(LEcols)>=1:
                        #LEcols.remove(target_variable) 
                        data,data_le = DataEncoding.Label_Encode_Dec(lencm,data,LEcols);             
                        target_variable = list(data_le.columns)[0]
           
                
            drop_cols = OHEcols + LEcols        
            
            if drop_cols:
                data_t = data.drop(drop_cols,axis=1)
            else:
                data_t = data
            utils.logger(logger, correlationId, 'INFO', ('Encoding completed and aggregation started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(uniqueId))

            dbproconn,dbprocollection = utils.open_dbconn("DE_DataProcessing")
            data_json = list(dbprocollection.find({"CorrelationId" :correlationId})) 
            if EnDeRequired:
                 t = base64.b64decode(data_json[0]['DataModification'])
                 data_json[0]['DataModification'] = eval(EncryptData.DescryptIt(t))   
            interapolationTechnique = data_json[0]['DataModification']['Features']['Interpolation']
            #print (data_t)
            data_t = aggregation.main(correlationId,data_t,DateCol,target_variable,interapolationTechnique,freq=[frequency],agg=agg.lower())
            #print (data_t)
            utils.logger(logger, correlationId, 'INFO', ('Aggregation Completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(uniqueId))

            ##save data to preprocessing
        utils.logger(logger, correlationId, 'INFO', ('Data Merge Started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(uniqueId))

        olddata = utils.data_timeseries(correlationId,'DE_PreProcessedData')[frequency]
        
        olddata.set_index(olddata[DateCol],drop=True,inplace=True)
        olddata.index = pd.to_datetime(olddata.index)
        olddata.sort_index(inplace=True)
        #df = pd.DataFrame()
        if Data!="null" or data_t:
            #print ("abcd",data_t)
            lastDate = olddata.iloc[[-1]].index.tolist()[0].tz_localize(None)
            
            firstDate = data_t[frequency].iloc[[0]].index.tolist()[0].tz_localize(None)
            #print ((firstDate-lastDate).days)
            if (firstDate-lastDate).days < -1:
                if AppID not in ["595fa642-5d24-4082-bb4d-99b8df742013","a3798931-4028-4f72-8bcd-8bb368cc71a9","9fe508f7-64bc-4f58-899b-78f349707efa"]:
                    raise Exception( "Have data till "+lastDate.strftime("%m/%d/%Y %H:%M:%S")+". Kindly sent data since then")
                else:
                    if (firstDate-lastDate).days/freqRange[frequency] >1 :
                        raise Exception( "last entry of data present on "+lastDate.strftime("%m/%d/%Y %H:%M:%S")+". Kindly sent data since then"   )
                    if (firstDate == lastDate):
                        try:
                            data_t[frequency].drop(firstDate,inplace=True)
                        except:
                            utils.logger(logger, correlationId, 'INFO', ('Dropping first date failed in if function'),str(uniqueId))
                   
                    if not data_t[frequency][(data_t[frequency].index > lastDate)].empty:
                        #data_t[frequency] = data_t[frequency][(data_t[frequency].index > lastDate)]
                        data_t[frequency][DateCol]= data_t[frequency].index
                        df = pd.concat([olddata, data_t[frequency]],sort=True)
                    else:
                        df = olddata
            else:
                #print("gg27",(firstDate-lastDate).days < -1)
                if (firstDate-lastDate).days/freqRange[frequency] >1:
                        raise Exception( "last entry of data present on "+lastDate.strftime("%m/%d/%Y %H:%M:%S")+". Kindly sent data since then"   )
                if (firstDate == lastDate):
                    try:
                        data_t[frequency].drop(firstDate,inplace=True)
                    except:
                        utils.logger(logger, correlationId, 'INFO', ('Dropping first date failed in else function'),str(uniqueId))

                data_t[frequency][DateCol]= data_t[frequency].index
                df = pd.concat([olddata, data_t[frequency]],sort=True)
                #df = olddata
        else:        
            df = olddata
        df.set_index(df[DateCol],drop=True,inplace=True)
        try:
            df.index = pd.to_datetime(df.index)
        except ValueError:
            df.index = pd.to_datetime(df.index,utc=True)
        df.sort_index(inplace=True)
        #print(df)
        df.fillna(0, inplace = True) 
        df.dropna(inplace=True)
        utils.logger(logger, correlationId, 'INFO', ('Data Merge Completed and Refit started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(uniqueId))

        dbconn,dbcollection = utils.open_dbconn('SSAI_savedModels')
        data_json = dbcollection.find({"CorrelationId":correlationId,
                                  "FileName":selectedModel,     
                                  "FileType":'MLDL_Model'})
        dbconn.close()
        cfg =data_json[0].get('Configuration')
        history = df[df.columns.difference([DateCol])].astype('float32').values.flatten().tolist()
        if selectedModel.split('_')[0] in ["ARIMA"]:
           try: 
               MLDL_Model =  ARIMA(history, order=cfg["order"]).fit()
           except ValueError:
               MLDL_Model =  SARIMAX(history, order=cfg["order"],seasonal_order=(0,0,0,0),enforce_stationarity=False,enforce_invertibility=False).fit()
               selectedModel = "SARIMA_w"
        elif selectedModel.split('_')[0] in ["SARIMA"]:
           try:
                MLDL_Model =  SARIMAX(history, order=cfg["order"],seasonal_order=cfg["seasonal_order"],trend = cfg["trend"],enforce_stationarity=False,enforce_invertibility=False).fit()
           except ValueError:
                MLDL_Model =  SARIMAX(history, order=cfg["order"],seasonal_order=[2,2,2,2],trend = cfg["trend"],enforce_stationarity=False,enforce_invertibility=False).fit()
		   
        elif selectedModel.split('_')[0] in ["ExponentialSmoothing"]:
            MLDL_Model = ExponentialSmoothing(np.asarray(df[df.columns.difference([DateCol])]), trend=cfg[0], damped=cfg[1], seasonal=None).fit(optimized=True, use_boxcox=False, remove_bias=cfg[3])#cfg[2]
        elif selectedModel.split('_')[0] in ["Holt-Winters"]:
            if "smoothing_level" not in cfg.keys():
                MLDL_Model = ExponentialSmoothing(history,seasonal_periods=cfg["seasonal_periods"], trend=cfg["trend"], seasonal=cfg["seasonal"],damped = cfg["damped"]).fit(use_boxcox=False)
            else:
                MLDL_Model = ExponentialSmoothing(history,seasonal_periods=cfg["seasonal_periods"], trend=cfg["trend"], seasonal=cfg["seasonal"],damped = cfg["damped"]).fit(use_boxcox=False,smoothing_level=cfg["smoothing_level"], smoothing_slope=cfg["smoothing_slope"])
        #elif selectedModel.split('_')[0] in ["Prophet"]:	
            #MLDL_Model=Prophet(interval_width=0.95) 
            #_df = df.copy()
            #_df[DateCol] = df[DateCol].index.tz_localize(None)
            #_df.rename(columns = {DateCol:"ds",df.columns.difference([DateCol])[0]:"y"},inplace=True)

            #MLDL_Model.fit(_df)
        utils.logger(logger, correlationId, 'INFO', ('Refit Completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(uniqueId))

        if AppID == "9fe508f7-64bc-4f58-899b-78f349707efa":
            last_trained_date = df.iloc[[-1]].index.tolist()[0]#olddata[DateCol][0]
            dbconn,dbcollection = utils.open_dbconn('SSAI_PublishModel')
            data_json = list(dbcollection.find({"CorrelationId":correlationId,"UniqueId":uniqueId}))
            StartDates = [datetime.strptime(each,"%d-%m-%Y") for each in data_json[0]['StartDates']]
            StartDates.sort()
            last_predict_date = StartDates[-1]
            steps = round((last_predict_date - pd.to_datetime(last_trained_date).tz_localize(None)).days /int(aggDays))+1
        else:
            steps = int(f[frequency])														   
        #steps = int(f[frequency])
        freq = freqDict[frequency]
        if frequency == "CustomDays":
            if AppID in ["595fa642-5d24-4082-bb4d-99b8df742013","a3798931-4028-4f72-8bcd-8bb368cc71a9","9fe508f7-64bc-4f58-899b-78f349707efa"]:
                freq = str(aggDays)+freq
            else:
                freq = str(value)+freq
        #print ("Stepsss",steps)    
        #print (selectedModel)
        utils.logger(logger, correlationId, 'INFO', ('Model.forecast Started At '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(uniqueId))
            
        if selectedModel.split('_')[0] in ["ARIMA"]: 
                forecasted = [max(0.0,round(float(each),2)) for each in MLDL_Model.forecast(steps)[0].tolist()]
                upperbound = [each[0] for each in MLDL_Model.forecast(steps)[2].tolist()]
                lowerbound = [each[1] for each in MLDL_Model.forecast(steps)[2].tolist()]
        elif selectedModel.split('_')[0] in ["SARIMA"]:
                forecast= MLDL_Model.get_forecast(steps)
                forecasted =[max(0.0,round(float(each),2)) for each in forecast.predicted_mean]
                upperbound = [each[1] for each in forecast.conf_int()]
                lowerbound = [each[0] for each in forecast.conf_int()]
        #elif selectedModel.split('_')[0] in ["Prophet"]:
                #future = MLDL_Model.make_future_dataframe(periods = steps,freq=freq)
                #forecastdf = MLDL_Model.predict(future)
                #forecasted = [max(0,round(float(each),2)) for each in forecastdf['yhat'].tolist()[:steps]]
                #upperbound = forecastdf['yhat_upper'].tolist()[:steps]
                #lowerbound = forecastdf['yhat_lower'].tolist()[:steps]				
        else:
                fitModel = MLDL_Model
                forecasted = [max(0.0,round(float(each),2)) for each in fitModel.forecast(steps).tolist()]
                z = 1.96
                sse = fitModel.sse
                lowerbound = forecasted - z * np.sqrt(sse/len(forecasted))
                upperbound  = forecasted + z * np.sqrt(sse/len(forecasted))
        #print (forecasted,lowerbound,upperbound)
        

        if frequency=="Hourly":
            RangeTime = pd.date_range(df.iloc[[-1]].index.tolist()[0], periods=steps+1,freq=freq).strftime('%d-%m-%Y %H:%M:%S').astype('str').tolist()[1:]
        else:
            #print ("@@$@$@$",df.iloc[[-1]].index.tolist()[0],steps+1,freq)
            RangeTime = pd.date_range(df.iloc[[-1]].index.tolist()[0], periods=steps+1,freq=freq).strftime('%d-%m-%Y').astype('str').tolist()[1:]
        utils.logger(logger, correlationId, 'INFO', ('Model.forecast Completed At '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(uniqueId))

        if AppID == "9fe508f7-64bc-4f58-899b-78f349707efa":
            temp_forecast = []
            temp_upper = []
            temp_lower=[]
            #RangeTime = StartDates
            #print ("Hererere",RangeTime,StartDates)
            for d in StartDates:
                diff = [abs(round(diff_days(d,each))) for each in RangeTime]
                temp_forecast.append(forecasted[diff.index(min(diff))])
                temp_upper.append(upperbound[diff.index(min(diff))])
                temp_lower.append(lowerbound[diff.index(min(diff))])
            forecasted = temp_forecast
            RangeTime = [date.strftime('%d-%m-%Y') for date in StartDates]
            upperbound = temp_upper
            lowerbound = temp_lower
            
        forecast_df = pd.DataFrame([],columns=[DateCol,target_variable])
        forecast_df[DateCol] = RangeTime
        forecast_df[target_variable] = forecasted
        forecast_df["UpperBound"]=upperbound
        forecast_df["LowerBound"]=lowerbound
        forecast_df["ConfidenceInterval"] = "95%"
        predicted = forecast_df.to_json(orient="records")
        #print ("Predicted",predicted)
        if EnDeRequired:                                  #EN555.................................
            predicted = EncryptData.EncryptIt(predicted)
        dbconn,dbcollection = utils.open_dbconn('SSAI_PublishModel')
        if prediction == False:  
            dbcollection.update_one({"CorrelationId":correlationId,"UniqueId":uniqueId},{'$set':{         
                                    'PredictedData' : predicted                                                      
                                   }}) 
            dbcollection.update_one({"CorrelationId":correlationId,"UniqueId":uniqueId},{'$set':{         
                                    'Status' : "C" , 'Progress':"100"                                                    
                                   }})
        else:
            dbcollection.update_one({"CorrelationId":correlationId,"UniqueId":uniqueId,"Chunk_number":chunk_number},{'$set':{         
                                    'PredictedData' : predicted                                                      
                                   }}) 
            dbcollection.update_one({"CorrelationId":correlationId,"UniqueId":uniqueId,"Chunk_number":chunk_number},{'$set':{         
                                    'Status' : "C" ,'Progress':"100"                                                     
                                   }})
            
        dbconn.close()
        
    #return forecast_df.to_json(orient="records")      
    else:
        raise Exception ("Unable to find model for corresponding correlationId")

#correlationId = "b786c2e7-aa4e-4e6b-8278-866fbe152c60"
####rainedModelId = "1c623a84-086c-4679-8615-38b786acd181"
#frequency="Daily"
#uniqueId="46642b92-3549-4c08-885f-b3fa3a761134"
#pageInfo = "ForecastModel"
#userId=1
##Data="null"
####Data = [{'Date': Timestamp('1972-01-14 00:00:00'), 'Sales_mean': 2815, 'id': 1}, {'Date': Timestamp('1972-01-15 00:00:00'), 'Sales_mean': 2672, 'id': 2}]
##print (main(correlationId,frequency,Data,pageInfo="forecastModel"))
##correlationId = "3525eebb-8b36-4862-80bd-4396210c9892"
##uniqueId = "f89362fb-ce6b-42e0-820d-36a2088d3e88"
##pageInfo="ForecastModel"
##userId=1

try:
    correlationId = sys.argv[1]
    uniqueId = sys.argv[2]
    pageInfo = sys.argv[3]
    userId = sys.argv[4]
    logger = utils.logger('Get',correlationId)
    utils.logger(logger, correlationId, 'INFO', ('import took '  + str(end-start)+' secs' + " with cpu:"+cpu+" with memory "+memory),str(uniqueId))
    utils.logger(logger, correlationId, 'INFO', ('Started Forecast at time '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(uniqueId))
    utils.updQdb(correlationId,'P','In Progress',pageInfo,userId,UniId = uniqueId)
    main_wrapper(correlationId,uniqueId,pageInfo)
    utils.logger(logger, correlationId, 'INFO', ('Forecast Completed at time '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(uniqueId))
    utils.updQdb(correlationId,'C','Task Completed',pageInfo,userId,UniId = uniqueId)
    
except Exception as e:
    logger = utils.logger('Get',correlationId)
    utils.logger(logger,correlationId,'ERROR','Trace',str(uniqueId))
    utils.updateErrorInTable(e.args[0], correlationId, uniqueId)
    utils.updQdb(correlationId,'E','ERROR',pageInfo,userId,UniId = uniqueId)
    utils.save_Py_Logs(logger,correlationId)
    sys.exit()
    #return jsonify({'status': 'false', 'message':str(e.args)}), 200
else:
    utils.logger(logger,correlationId,'INFO','Task scheduled')
    utils.save_Py_Logs(logger,correlationId)
    sys.exit()
