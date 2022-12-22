# -*- coding: utf-8 -*-
"""
Created on Mon Feb  8 15:56:48 2021

@author: shrayani.mondal
"""
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
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
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


import pandas as pd
import datetime
from SSAIutils import utils
from main import publishModel_C
from pandas import Timestamp
end = time.time()
'''
correlationId = "3a198282-484a-4102-8c70-3861cbeb0959"
cascadeCorrelationID = "ca6aa8fb-c5ec-4c5c-9645-7c3acc0fbd2e"
uniqueId = "4561d3d2-5d10-45a2-9918-3be2492947ab"
pageInfo = "PublishModel"
userId = "shrayani.mondal@ds.dev.accenture.com"

'''
def getFMVisualization(data_M1,Model2_results,ReleaseEndDate_dict,CRPredictionDf):
    visualization = []
    for item in ReleaseEndDate_dict:
        temp_dict = {}
        temp_dict["date"] = item["Release End Date"].to_pydatetime().strftime("%d/%m/%Y %H:%M:%S %Z")
        temp_dict["successProbability"] = pd.DataFrame(Model2_results)[["Release Name","predictedValue"]].set_index("Release Name").loc[item["Release Name"]]["predictedValue"]
        temp_dict["releaseName"] = str(item["Release Name"])
        temp_featurewt = pd.DataFrame(Model2_results)[["Release Name","FeatureWeights"]].set_index("Release Name").loc[item["Release Name"]].iloc[0]
        temp_dict["observations"] = sorted(temp_featurewt, key=temp_featurewt.get, reverse=True)[:3]
        
        changeReqList = []
        temp_df = CRPredictionDf[CRPredictionDf["Release Name"]==item["Release Name"]]
        temp_df.drop("Release Name", axis=1, inplace=True)
        
        reqCols = [col for col in data_M1.columns if col not in ["ChangeOutcome","SNChangeNumber"]]
        for indx,row in enumerate(temp_df.iterrows()):
            changeReqDict = {}
            changeReqDict["CRVal"] = str(round(row[1]["CRVal"],3))
            changeReqDict["ChangeNumber"] = row[1]["ChangeNumber"]
            changeReqDict["ChangeOutcome"] = row[1]["ChangeOutcome"]
            InfluencersM1 = []
            for col in reqCols:
                InfluencersM1Dict = {}
                InfluencersM1Dict["name"] = str(col)
                InfluencersM1Dict["value"] = str(data_M1.loc[data_M1["SNChangeNumber"] == row[1]["ChangeNumber"], col].iloc[0])
                InfluencersM1Dict["featureWeight"] = str(round(row[1]["FeatureWeights"].get(col, 0),3))
                InfluencersM1.append(InfluencersM1Dict)
            changeReqDict["InfluencersM1"] = InfluencersM1
            changeReqList.append(changeReqDict)
        temp_dict["changeReqArr"] = changeReqList
        visualization.append(temp_dict)
    return visualization
    
def main(correlationId,cascadeCorrelationID,uniqueId,pageInfo,userId):
    
    try:
        
        #check if the correationIds belong to the same FM model
        if utils.isFMModel(cascadeCorrelationID):
            FMModel = "FMModel"
        else:
            raise Exception( "The payload is incorrect. Please check")
            
        #for first model data is the ingested data minus the target column
        data_M1 = utils.getFMPredictionData(correlationId,cascadeCorrelationID,uniqueId)
        #data_M1 = utils.data_from_chunks(corid = cascadeCorrelationID, collection = "PS_IngestedData")
        
        #instead of calling main wrapper, call main
        Model1_results = publishModel_C.main(data_M1.copy(),0,cascadeCorrelationID,uniqueId,FMModel,prediction=False,mapped_column=None,unique_identifer=None)
        
        #get data for the second model
        cascadeResult = utils.getFMPredictedCols(Model1_results)
        cascadeResultDf = pd.json_normalize(cascadeResult, sep = '_')
        
        data_M2 = utils.getFMTransformedData(data_M1.copy(),cascadeResultDf.copy())
        if isinstance(data_M2,str):
            raise Exception(data_M2)
        
        #Now get prediction from 2nd model
        Model2_results = publishModel_C.main(data_M2.copy(),0,correlationId,uniqueId,FMModel,prediction=False,mapped_column=None,unique_identifer=None)
        
        ReleaseEndDate_dict = data_M2[["Release Name","Release End Date"]].to_dict(orient="records")
        ReleaseEndDate_dict = sorted(ReleaseEndDate_dict, key = lambda i: i["Release End Date"],reverse=False)

        CRPredictionDf = cascadeResultDf.copy()
        CRPredictionDf = pd.merge(data_M1[["Release Name","SNChangeNumber"]], CRPredictionDf, left_on = "SNChangeNumber", right_on = "ID", suffixes=(False, False))
        CRPredictionDf.drop(["TargetScore_Missed","SNChangeNumber"], axis=1, inplace= True)
        CRPredictionDf = pd.merge(pd.DataFrame(Model1_results)[["FeatureWeights","SNChangeNumber"]], CRPredictionDf, left_on = "SNChangeNumber", right_on = "ID", suffixes=(False, False))
        CRPredictionDf.drop(["SNChangeNumber"], axis=1, inplace= True)
        CRPredictionDf.rename(columns={"TargetScore_Met":"CRVal",
                                       "ID":"ChangeNumber",
                                       "Target":"ChangeOutcome"}, inplace = True)
        
        visualization = getFMVisualization(data_M1.copy(),Model2_results,ReleaseEndDate_dict,CRPredictionDf.copy())
        
        #insert visualization
        dbconn,dbcollection = utils.open_dbconn('SSAI_FMVisualization')
        dbcollection.update_many({"CorrelationId": correlationId,"UniqueId":uniqueId},
                            { "$set":{      
                               "CreatedByUser"   : userId,
                               "CreatedOn"       : str(datetime.datetime.now()),
                               "Visualization"   : visualization
                               }},upsert=True)
        dbconn.close()
        
    except Exception as e:
        raise Exception(e.args[0])
try:
    
    correlationId = sys.argv[1]
    cascadeCorrelationID = sys.argv[2]
    uniqueId = sys.argv[3]
    pageInfo = sys.argv[4]
    userId = sys.argv[5]
    logger = utils.logger('Get',correlationId)
    requestId = uniqueId
    utils.updQdb(correlationId,'P','0',pageInfo,userId,UniId = uniqueId)
    utils.logger(logger, correlationId, 'INFO', ('import took '  + str(end-start) + ' secs'+" with CPU: "+cpu+" Memory: "+memory),str(requestId))
    utils.logger(logger,correlationId,'INFO',
                 ('FM Prediction Starts: Arguments '+str(sys.argv)+
                  ' CorrelationID :'+str(correlationId)+
                  ' CorrelationID of 1st model :'+str(cascadeCorrelationID)+
                   ' UniqueID :'+str(uniqueId)+
                  ' pageInfo :'+pageInfo+
                  ' UserId :'+userId),str(requestId))
    utils.logger(logger, correlationId, 'INFO', ('FM prediction started at '  + str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
    main(correlationId,cascadeCorrelationID,uniqueId,pageInfo,userId)
    utils.logger(logger, correlationId, 'INFO', ('FM prediction completed at '  + str(datetime.datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
    utils.updQdb(correlationId,'C','100',pageInfo,userId,UniId = uniqueId)
except Exception as e:
    correlationId='Error_corrid'
    uniqueId='Error_UniqueId'
    logger = utils.logger('Get',correlationId)
    utils.logger(logger,correlationId,'ERROR','Trace',str(requestId))
    utils.updateErrorInTable(e.args[0], correlationId, uniqueId)
    utils.updQdb(correlationId,'E',e.args[0],pageInfo,userId,userId,UniId = uniqueId)
    utils.save_Py_Logs(logger,correlationId)
    sys.exit()
    
else:
    utils.logger(logger,correlationId,'INFO','Task scheduled',str(requestId))
    utils.save_Py_Logs(logger,correlationId)
    sys.exit()
    
             
             


    
