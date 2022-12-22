# -*- coding: utf-8 -*-
"""
Created on Tue Jul  7 15:07:39 2020

@author: s.siddappa.dinnimani
"""



import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
#from cryptography.fernet import Fernet
##corr = "0feb4ea1-4a1c-4835-b970-35eae5b292ff"
import platform
if platform.system() == 'Linux':
    conf_path = '/IngrAIn_Python/main/pythonconfig.ini'
    conf_path_pheonix = '/IngrAIn_Python/main/pheonixentityconfig.ini'
    work_dir = '/IngrAIn_Python'
    modelPath = '/IngrAIn_Python/saveModels/'
    # pylogs_dir = '/IngrAIn_Python/tranPyLogs/'
elif platform.system() == 'Windows':
    conf_path = '\IngrAIn_Python\main\pythonconfig.ini'
    conf_path_pheonix = '\IngrAIn_Python\main\pheonixentityconfig.ini'
    work_dir = '\IngrAIn_Python'
    modelPath = '\IngrAIn_Python\saveModels\\'

import configparser, os

mainPath = os.getcwd() + work_dir
import sys
from pythonjsonlogger import jsonlogger

sys.path.insert(0, mainPath)
sys.path.insert(0, mainPath + '/main')
import pickle
from SSAIutils import utils
import pandas as pd
from cryptography.fernet import Fernet
from main import file_encryptor
from datetime import datetime,timedelta
config = configparser.RawConfigParser()
configpath = str(os.getcwd()) + str(conf_path)
try:
    config.read(configpath)
except UnicodeDecodeError:
    config = file_encryptor.get_configparser_obj(configpath)


config = configparser.RawConfigParser()
configpath = str(os.getcwd()) + str(conf_path)
try:
    config.read(configpath)
except UnicodeDecodeError:
    config = file_encryptor.get_configparser_obj(configpath)

time_days = 180
collection_list = ["AICoreModels","AICore_Preprocessing","AICustomContraints","AICustomDataSource","AISavedUsecases","AIServiceAutoTrainLog","AIServiceIngestData",
"AIServiceRecordsDetails","AIServiceRequestBatchLimitMonitor","AIServiceRequestStatus","AIServicesIntentEntityPrediction","AIServicesPrediction",
"AIServicesSentimentPrediction","AIServicesTextSummaryPrediction","AppIntegration","AppIntegration23","AppNotificationLog","AssetUsageTracking",
"AuditTrailLog","AutoReTrainTasks","AutoTrainModelTasks","CallBackErrorLog","CallBackResponseData","Clustering_BusinessProblem","Clustering_DE_DataCleanup",
"Clustering_DE_PreProcessedData","Clustering_DataCleanUP_FilteredData","Clustering_DataPreprocessing","Clustering_Eval","Clustering_EvalTestResults",
"Clustering_IngestData","Clustering_SSAI_savedModels","Clustering_StatusTable","Clustering_TrainedModels","Clustering_ViewMappedData","Clustering_ViewTrainedData",
"Clustering_Visualization","CustomConfigurations","DE_AddNewFeature","DE_AddNewFeature_archive","DE_DataCleanup","DE_DataCleanup_archive","DE_DataProcessing",
"DE_DataProcessing_archive","DE_NewFeatureData","DE_NewFeatureData_archive","DE_PreProcessedData","DE_PreProcessedData_archive","DataCleanUP_FilteredData",
"DataCleanUP_FilteredData_archive","DataSetInfo","DataSet_IngestData","ENSEntityNotificationLog","IERequestBatchLimitMonitor","IngrainDeliveryConstruct",
"InstaLog","Insta_AutoLog","ME_FeatureSelection","ME_FeatureSelection_archive","ME_HyperTuneVersion","ME_HyperTuneVersion_archive","ME_RecommendedModels",
"ME_RecommendedModels_archive","MLDL_ModelsMaster","ManualArchivalTasks","ModelMetrics","PS_BusinessProblem","PS_BusinessProblem_archive","PS_IngestedData",
"PS_IngestedData_archive","PS_MultiFileColumn","PS_UsecaseDefinition","PS_UsecaseDefinition_archive","PhoenixClients","PhoenixDeliveryConstructs","PhoenixIterations",
"PredictionSchedulerLog","PrescriptiveAnalyticsResults","PublicTemplateMapping","QueueMonitor","SSAI_CascadeVisualization","SSAI_CascadedModels","SSAI_CustomContraints",
"SSAI_CustomDataSource","SSAI_DeployedModels","SSAI_DeployedModels5","SSAI_DeployedModels_archive","SSAI_IngrainRequests","SSAI_LogAutoTrainedFeatures",
"SSAI_PredictedData","SSAI_PublicTemplates","SSAI_PublishModel","SSAI_RecommendedTrainedModels","SSAI_RequestBatchLimitMonitor","SSAI_UseCase",
"SSAI_WorkerServiceJobs","SSAI_savedModels","SSAI_savedModels_archive","Services","TrainedModelHistory","WF_IngestedData","WF_TestResults","WhatIfAnalysis"]

##############SELFSERVICE-COMMON CORS
dbconn, dbcollection = utils.open_dbconn("SSAI_DeployedModels")
all_cors_details =  list(dbcollection.find({"CreatedOn": {'$lt': (datetime.now()- timedelta(days=time_days)).strftime('%Y-%m-%d %H:%M:%S')}},{"CorrelationId":1,"CreatedOn":1}))
#print(all_cors_details)
all_cors = [item["CorrelationId"] for item in all_cors_details]
#print(all_cors)
#print(all_cors[1])
#print(len(all_cors))
#all_cors = [1,2,3,4]

dbconn, dbcollection = utils.open_dbconn("PublicTemplateMapping")
all_templates =  list(dbcollection.find({},{"UsecaseID":1}))
all_template_cors = [item["UsecaseID"] for item in all_templates]
#print(all_template_cors)
#print(len(all_template_cors)) 
#all_template_cors = [3,4]

all_cors_set = set(all_cors)
all_template_cors_set = set(all_template_cors)
common_cors_selfservice = all_cors_set - (all_cors_set & all_template_cors_set)
print(common_cors_selfservice)

#########################AISERVICE-COMMON CORS
dbconn, dbcollection = utils.open_dbconn("AICoreModels")
all_cors_details =  list(dbcollection.find({"CreatedOn": {'$lt': (datetime.now()- timedelta(days=time_days)).strftime('%Y-%m-%d %H:%M:%S')}},{"CorrelationId":1,"CreatedOn":1}))
#print(all_cors_details)
all_cors = [item["CorrelationId"] for item in all_cors_details]
#print(all_cors)
#print(all_cors[1])
#print(len(all_cors))

dbconn, dbcollection = utils.open_dbconn("AISavedUsecases")
all_templates =  list(dbcollection.find({},{"CorrelationId":1}))
all_template_cors = [item["CorrelationId"] for item in all_templates]
#print(all_template_cors)
#print(len(all_template_cors)) 

all_cors_set = set(all_cors)
all_template_cors_set = set(all_template_cors)
common_cors_aiservice = all_cors_set - (all_cors_set & all_template_cors_set)
print(common_cors_aiservice)

#########################CLUSTERING-COMMON CORS
dbconn, dbcollection = utils.open_dbconn("Clustering_StatusTable")
all_cors_details =  list(dbcollection.find({"ModifiedOn": {'$lt': (datetime.now()- timedelta(days=time_days)).strftime('%Y-%m-%d %H:%M:%S')}},{"CorrelationId":1,"CreatedOn":1}))
#print(all_cors_details)
all_cors = [item["CorrelationId"] for item in all_cors_details]
#print(all_cors)
#print(all_cors[1])
#print(len(all_cors))

dbconn, dbcollection = utils.open_dbconn("AISavedUsecases")
all_templates =  list(dbcollection.find({},{"CorrelationId":1}))
all_template_cors = [item["CorrelationId"] for item in all_templates]
#print(all_template_cors)
#print(len(all_template_cors)) 

all_cors_set = set(all_cors)
all_template_cors_set = set(all_template_cors)
common_cors_clustering = all_cors_set - (all_cors_set & all_template_cors_set)
print(common_cors_clustering)

###########FINAL LIST
final_list = list(common_cors_selfservice)+list(common_cors_aiservice)+list(common_cors_clustering)
############DOCUMENTS REMOVAL
if len(final_list)>0:
    for item in collection_list:
        dbconn, dbcollection = utils.open_dbconn(item)
        dbcollection.remove({"CorrelationId":{'$in':final_list}})