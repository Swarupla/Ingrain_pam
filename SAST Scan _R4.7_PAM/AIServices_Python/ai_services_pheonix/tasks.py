# -*- coding: utf-8 -*-
"""
Created on Mon Jul 27 16:18:14 2020

@author: s.siddappa.dinnimani
"""
import time
start=time.time()
#from SSAIutils import encryption
import pandas as pd
import invokeIngestData
#import BestDeveloper
import similarity_analytics
#import Next_Word
import time
import os,sys,inspect,platform
import text_summarization
currentdir = os.path.dirname(os.path.abspath(inspect.getfile(inspect.currentframe())))
parentdir = os.path.dirname(currentdir)
sys.path.insert(0,parentdir) 
import utils
import configparser
from pathlib import Path

mainDirectory = str(Path(__file__).parent.parent.absolute())
import file_encryptor
config = configparser.RawConfigParser()
configpath = mainDirectory+"/pythonconfig.ini"
try:
    config.read(configpath)
except UnicodeDecodeError:
    config = file_encryptor.get_configparser_obj(configpath)
summary_path=config['TextSummary']['ModelsummaryPath']
if platform.system() == 'Linux':
    configpath = mainDirectory+"/pythonconfig.ini"
    EntityConfigpath = mainDirectory+"/pheonixentityconfig.ini"
    thai_model_path='fasttext_model/cc.th.100.bin'
    if config['config']['path']=='/app/':
        pythonPath="python"
    else:
        pythonPath = mainDirectory+"/IngrAInAIServices_env/bin/python"
elif platform.system() == 'Windows':
    pythonPath = mainDirectory+"\\venv\\Scripts\\python"
    configpath = mainDirectory+"\\pythonconfig.ini"
    EntityConfigpath = mainDirectory+"\\pheonixentityconfig.ini"
    thai_model_path='fasttext_model\\cc.th.100.bin'

import spacy
import json
from threading import Thread
from queue import Queue
import fasttext,fasttext.util
import gc
from flask import Flask,request,jsonify

def load_model(language):
    #global spacy_vectors 
    #print("session2:: ", app.config['sv'])
    #print("hexa adress3::", hex(id(spacy_vectors)))
    if language in spacy_vectors.keys():
        spacy_vectors[language]['time'] = time.time()
        #q.put(None)
        #q.put(spacy_vectors)
        #print("83",q.get())
    else: 
        if language =='english':
            try: 
                spacy_vectors[language] = dict({'model':spacy.load('en_core_web_lg'),
                                            'time' : time.time()})
                #spacy_vectors['german'] = dict({'model':spacy.load('en_core_web_lg'),
                #                            'time' : time.time()})											
                #spacy_vectors_en = spacy.load('en_core_web_lg')
                #q.put(spacy_vectors)
                #time.sleep(1)	
            except Exception as e:
                error_encounterd = str(e.args[0])
        elif language =='spanish':
            try: 
                spacy_vectors[language] = dict({'model':spacy.load('es_core_news_lg'),
                                            'time' : time.time()})
                #spacy_vectors_es = spacy.load('es_core_news_lg')
                #q.put(spacy_vectors)
                #time.sleep(1)	
            except Exception as e:
                error_encounterd = str(e.args[0])
        elif language =='portuguese':
            try: 
                spacy_vectors[language] = dict({'model':spacy.load('pt_core_news_lg'),
                                            'time' : time.time()})
                #spacy_vectors_pt = spacy.load('pt_core_news_lg')
                #q.put(spacy_vectors)
                #time.sleep(1)	
            except Exception as e:
                error_encounterd = str(e.args[0])
        elif language =='german':
            try: 
                spacy_vectors[language] = dict({'model':spacy.load('de_core_news_lg'),
                                            'time' : time.time()})
                #spacy_vectors_de = spacy.load('de_core_news_lg')
                #q.put(spacy_vectors)
                #time.sleep(1)	
            except Exception as e:
                error_encounterd = str(e.args[0])
        elif language =='chinese':
            try: 
                spacy_vectors[language] = dict({'model':spacy.load('zh_core_web_lg'),
                                            'time' : time.time()})
                #spacy_vectors_zh = spacy.load('zh_core_web_lg')
                #q.put(spacy_vectors)
                #time.sleep(1)	
            except Exception as e:
                error_encounterd = str(e.args[0])
        elif language =='japanese':
            try: 
                spacy_vectors[language] = dict({'model':spacy.load('ja_core_news_lg'),
                                            'time' : time.time()})
                #spacy_vectors_ja = spacy.load('ja_core_news_lg')
                #q.put(spacy_vectors)
                #time.sleep(1)	
            except Exception as e:
                error_encounterd = str(e.args[0])
        elif language =='french':
            try: 
                spacy_vectors[language] = dict({'model':spacy.load('fr_core_news_lg'),
                                            'time' : time.time()})
                #spacy_vectors_fr = spacy.load('fr_core_news_lg')
                #q.put(spacy_vectors)
                #time.sleep(1)	
            except Exception as e:
                error_encounterd = str(e.args[0])
        elif language =='thai':
            try: 
                spacy_vectors[language] = dict({'model':fasttext.load_model(thai_model_path),
                                            'time' : time.time()})
                #spacy_vectors_thai = fasttext.load_model(thai_model_path)
                #q.put(spacy_vectors)
                #time.sleep(1)	
            except Exception as e:               
                error_encounterd = str(e.args[0])
    #remove_unused_model(q)
    #s.enter(int(time_limit),1,remove_unused_model,argument=q)
    #s.run()
	

global spacy_vectors
spacy_vectors= {}
#spacy_vectors = spacy.load('en_core_web_lg', disable=['tagger', 'parser','ner','lemmatizer'])
#load_model('english')
                  
end=time.time()

def InvokeSimilarityAnalytics(correlationId,pageInfo,userId,uniId,UseCase = None):
    start_time=time.time()
    logger = utils.logger('Get', correlationId)
    try:
        language=utils.GetLanguageForModel(correlationId)
        utils.logger(logger, correlationId, 'INFO',"language ::"+str(language) ,str(uniId))
        utils.logger(logger, correlationId, 'INFO',"spacy vectors before laoding ::"+str(spacy_vectors) ,str(uniId))
        load_model(language)
        utils.logger(logger, correlationId, 'INFO',"spacy vectors after laoding ::"+str(spacy_vectors) ,str(uniId))
        
        cpu,memory=utils.Memorycpu()
        utils.logger(logger, correlationId, 'INFO',str(pageInfo),str(uniId))
        utils.logger(logger, correlationId, 'INFO', ('import in tasks for invokesimilarity func took '  + str(end-start) + ' secs'+ " with CPU: "+cpu+" Memory: "+memory),str(uniId))
        if pageInfo == "TrainModel":
            utils.updateModelStatus(correlationId,uniId,"","InProgress","Model training in progress")
            utils.logger(logger, correlationId, 'INFO', ('Pageinfo with {} model training will commence'.format(pageInfo)),str(uniId))
            if UseCase == "NextWord":
                ret_val = Next_Word.train_nwp(correlationId,pageInfo,uniId)
            else:
                utils.logger(logger, correlationId, 'INFO', ('Pageinfo with {} entering train_similarity func within similarity_analytics'.format(pageInfo)),str(uniId))
                ret_val,message_error = similarity_analytics.train_similarity(correlationId,pageInfo,uniId,spacy_vectors)
            if ret_val:
                utils.updateModelStatus(correlationId,uniId,"","Completed","Model got created")
                utils.logger(logger, correlationId, 'INFO', ('Pageinfo with {} model training is complete'.format(pageInfo)),str(uniId))
                utils.updQdb(correlationId, 'C', '100', pageInfo, userId,uniId)
            else:
                if pageInfo == 'Retrain':
                    if message_error=='':
                        utils.updateModelStatus(correlationId,uniId,"","Error","Retrain Failed")
                        utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
                    else:
                        utils.updateModelStatus(correlationId,uniId,"","Error","Retrain Failed due to "+str(message_error))
                        errFlag = "ErrorReTrain"
                        utils.logger(logger, correlationId, 'INFO',str(errFlag) ,str(uniId))
                        raise Exception("Error in Retraining")
                else:
                    if message_error=='':
                        utils.updateModelStatus(correlationId,uniId,"","Error","Error in training")
                        utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
                    else:
                        utils.updateModelStatus(correlationId,uniId,"","Error",str(message_error))  
                        errFlag = "ErrorTrain"
                        utils.logger(logger, correlationId, 'INFO',str(errFlag) ,str(uniId))
                        raise Exception("Error in Training")
                        
        else:
            utils.logger(logger, correlationId, 'INFO', ('entering else loop of training tasks_py with pageinfo {}'.format(pageInfo)),str(uniId))
            print ('in1')
            IngestData_json, _,allparams = utils.getRequestParams(correlationId,pageInfo,uniId)
            print ('out1')
            utils.updQdb(correlationId, 'P', '5', pageInfo, userId,uniId)
            utils.logger(logger, correlationId, 'INFO', ('entering else loop of training tasks_py after request params with pageinfo {}'.format(pageInfo)),str(uniId))
            utils.logger(logger, correlationId, 'INFO', ('IngestData_json'+str(IngestData_json)),str(uniId))			
            if 'DataSetUId' in allparams:
                DataSetUId = allparams['DataSetUId']
            else:
                DataSetUId = False
            try:
                IngestData_json = eval(IngestData_json)
            except:
                IngestData_json = json.loads(IngestData_json)
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
            MappingColumns = {}
            errFlag = False
            if mapping_flag == 'False':
                utils.logger(logger, correlationId, 'INFO', ('Ingestion parameters in tasks'+str(IngestData_json)),str(uniId))
                utils.logger(logger, correlationId, 'INFO', "Ingestion started",str(uniId))
                if DataSetUId:
                    utils.logger(logger, correlationId, 'INFO', ('Ingesting_Data with DataSetUID'+str(DataSetUId)),str(uniId))
                    message = utils.update_ps_ingestdata(DataSetUId,correlationId,pageInfo,userId)
                    utils.logger(logger, correlationId, 'INFO', ('after data ingestion with DataSetUID'+str(message)),str(uniId))
                    utils.update_usecasedefeinition(DataSetUId,correlationId)
                    if message == "Ingestion completed":
                        utils.logger(logger, correlationId, 'INFO', ('Ingesting_Data' + '\t' + 'Source Type :' + str(message)),str(uniId))
                        utils.updQdb(correlationId, 'P', '25', pageInfo, userId,uniId)
                    else:
                        utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
                        utils.updQdb(correlationId, 'E', '100', pageInfo, userId,uniId)
                        raise Exception("Ingesting_Data failed with DataSetUID="+str(DataSetUId))
                    data_t = utils.data_from_chunks_offline_utility(corid=correlationId, collection="DataSet_IngestData",lime=None, recent=None,DataSetUId=DataSetUId)
                    utils.logger(logger, correlationId, 'INFO', ('after data_fromchunks_offline_utility_datasetUID' + str(data_t)),str(uniId))
                    if isinstance(data_t,pd.DataFrame):
                        cat_cols=utils.identifyDtype(data_t)  
                        filters_dict = dict(zip([i for i in cat_cols] , (list(data_t[i].dropna().astype(str).unique()) for i in cat_cols)))
                    else:
                        filters_dict={}
                    if len(filters_dict)>0:
                         for x,y in filters_dict.items():
                                list1=y
                                list2=len(y)*['False']
                                filters_dict[x]=dict(zip(list1,list2))
                else:
                    utils.logger(logger, correlationId, 'INFO', ('entering else loop of tasks where DataSetUID is not set and entering invokeingestdata'+str(DataSetUId)),str(uniId))
                    utils.updQdb(correlationId, 'P', '15', pageInfo, userId,uniId)
                    
                    MultiSourceDfs,MappingColumns,lastDateDict,errFlag =  invokeIngestData.Read_Data(correlationId,pageInfo,userId,uniId,IngestData_json)
                    utils.logger(logger, correlationId, 'INFO',str(errFlag) ,str(uniId))
                    utils.logger(logger, correlationId, 'INFO',str(MultiSourceDfs) ,str(uniId))
                    
                    if (errFlag == True):
                            
                            raise Exception("Error when ingesting the data")
                    
                    utils.updQdb(correlationId, 'P', '30', pageInfo, userId,uniId)                        
                        
                    #utils.logger(logger, correlationId, 'INFO', str(type(MultiSourceDfs['Custom'])),str(uniId))

                    filters_dict={}
                    for key,value in MultiSourceDfs.items():
                     for key1,value1 in value.items():   
                        if isinstance(value1,pd.DataFrame):
                            cat_cols=utils.identifyDtype(value1)  
                            filters_dict = dict(zip([i for i in cat_cols] , (list(value1[i].dropna().astype(str).unique()) for i in cat_cols)))
                        else:
                            filters_dict={}
                     if len(filters_dict)>0:
                         for x,y in filters_dict.items():
                                list1=y
                                list2=len(y)*['False']
                                filters_dict[x]=dict(zip(list1,list2))
                utils.save_filters_data(correlation_id,pageInfo,userId,filters_dict)
                utils.logger(logger, correlationId, 'INFO', ('entering else loop of tasks where DataSetUID is not set and entering #189 after data filter'+str(DataSetUId)),str(uniId))
                utils.updQdb(correlationId, 'P', '50', pageInfo, userId,uniId)
                if not DataSetUId:
                    if UseCase == "NextWord":
                        message,data = utils.save_data_multi_file(correlationId, pageInfo, userId, uniId, MultiSourceDfs, parent, mapping,
                                                             mapping_flag, ClientUID, DeliveryConstructUId, insta_id, auto_retrain,
                                                             datapre=None, lastDateDict=lastDateDict,MappingColumns= MappingColumns,usecase = "NextWord")
                    else:
                        utils.logger(logger, correlationId, 'INFO', ('entering else loop of tasks where DataSetUID is not set and entering save_data_multi_file'+str(DataSetUId)),str(uniId))
                        message,data = utils.save_data_multi_file(correlationId, pageInfo, userId, uniId, MultiSourceDfs, parent, mapping,
                                                                 mapping_flag, ClientUID, DeliveryConstructUId, insta_id, auto_retrain,
                                                                 datapre=None, lastDateDict=lastDateDict,MappingColumns= MappingColumns,usecase = "Similarity")
                        utils.updQdb(correlationId, 'C', '100', pageInfo, userId,uniId)
                        

                elif DataSetUId and UseCase == "NextWord":
                    columns = utils.CheckTextCols(data_t)
                    if len(columns) == 0:
                        if pageInfo == 'Retrain' or auto_retrain:
                            utils.updateModelStatus(correlationId,uniId,"","Completed","Retrain Failed")
                            utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
                        else:                       
                            utils.updateModelStatus(correlationId,uniId,"","Warning","There is no text columns for your data,please upload data with text columns")
                            utils.updQdb(correlationId, 'E', "ERROR",'Trace',str(uniId))
                            utils.logger(logger, correlationId, 'WARNING', "There is no text columns for your data,please upload data with text columns",str(uniId))
                            raise Exception("There is no text columns for your data,please upload data with text columns")

                utils.logger(logger, correlationId, 'INFO', "Ingestion Done...",str(uniId))
                if pageInfo in ["Ingest_Train","Retrain","Train_Predict"]:
                    utils.logger(logger, correlationId, 'INFO', ('Training will be initiated with {}'+str(pageInfo)),str(uniId))
                    utils.updateModelStatus(correlationId,uniId,"","Training is not initiated","upload is Success")
                    utils.updQdb(correlationId, 'P', '70', pageInfo, userId,uniId)
                    utils.updateModelStatus(correlationId,uniId,"","InProgress","Model training in progress")
                    #if pageInfo=='Train_Predict': #added new
                    #    ret_val = similarity_analytics.train_PredictSimilarity(correlationId,pageInfo,uniId)# new addition
                    if UseCase == "NextWord":
                        ret_val = Next_Word.train_nwp(correlationId,pageInfo,uniId)
                    else:
                        ret_val,message_error = similarity_analytics.train_similarity(correlationId,pageInfo,uniId,spacy_vectors)
                        
                    
                    if ret_val:
                        utils.updateModelStatus(correlationId,uniId,"","Completed","Model got created")
                        utils.updQdb(correlationId, 'C', '100', pageInfo, userId,uniId)
                        utils.logger(logger, correlationId, 'INFO', "Model creation is complete",str(uniId))
                    else:
                        if pageInfo == 'Retrain' or auto_retrain:
                            if message_error=='':
                                utils.updateModelStatus(correlationId,uniId,"","Error","Retrain Failed")
                                utils.updQdb(correlationId, 'E', 'Error in the ReTraining', pageInfo, userId,uniId)
                            else:
                                utils.updateModelStatus(correlationId,uniId,"","Error","Retrain Failed due to "+str(message_error))
                                utils.updQdb(correlationId, 'E', 'Error in the ReTraining', pageInfo, userId,uniId)
                            utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
                        else:
                            
                            if message_error=='':
                                utils.updateModelStatus(correlationId,uniId,"","Error","Error in training")
                                utils.updQdb(correlationId, 'E', 'Error in the Training', pageInfo, userId,uniId)
                            else:                                
                                utils.updateModelStatus(correlationId,uniId,"","Error",str(message_error))
                                utils.updQdb(correlationId, 'E', 'Error in the Training', pageInfo, userId,uniId)
                                utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
                            
                else:
                    if message == 'single_file' or DataSetUId:
                        utils.updateModelStatus(correlationId,uniId,"","Training is not initiated","upload is Success")
                        #utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
                        utils.updQdb(correlationId, 'C', '100', pageInfo, userId,uniId)
            
                    else:
                        utils.store_pickle_file(MultiSourceDfs, correlationId)
                        utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
                        utils.updQdb(correlationId, 'M', '100', pageInfo, userId,uniId)
            elif mapping_flag == 'True':
                utils.updQdb(correlationId, 'P', '55', pageInfo, userId,uniId)
                MultiSourceDfs = utils.load_pickle_file(correlationId)
                message,data = utils.save_data_multi_file(correlationId, pageInfo, userId, uniId, MultiSourceDfs, parent, mapping,
                                                         mapping_flag, ClientUID, DeliveryConstructUId, insta_id, auto_retrain,
                                                         datapre=None, lastDateDict=lastDateDict,MappingColumns= MappingColumns,usecase = None)
         
                
                utils.logger(logger, correlationId, 'INFO',('mapping flag with true tasks' + str(message)),str(uniId))
                utils.updQdb(correlationId, 'C', '100', pageInfo, userId,uniId)
    except Exception as e:
        if str(e.args[0]).__contains__('Data is not available for your selection'):
            utils.logger(logger, correlationId, 'INFO',str("Data is not available for your selection"),str(uniId))
        else:
            utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId)) 
        #utils.logger(logger, correlationId, 'INFO',str(errFlag) ,str(uniId))
        if (errFlag == False):
            utils.logger(logger, correlationId, 'INFO', 'Inside exception',str(uniId))
            utils.updateModelStatus(correlationId,uniId,"","Error","Error when Ingesting the data")
            utils.updQdb(correlationId, 'E', 'Error when Ingesting the data', pageInfo, userId,uniId)
            
        elif (errFlag == "ErrorTrain"):
            utils.logger(logger, correlationId, 'INFO', 'Inside Error in Train exception',str(uniId))
            #utils.updateModelStatus(correlationId,uniId,"","Error","Error in training the model")
            utils.updQdb(correlationId, 'E', 'Error in the Training', pageInfo, userId,uniId)
        elif (errFlag == "ErrorReTrain"):
            utils.logger(logger, correlationId, 'INFO', 'Inside Error in ReTrain exception',str(uniId))
            #utils.updateModelStatus(correlationId,uniId,"","Error","Error in training the model")
            utils.updQdb(correlationId, 'E', 'Error in the Retraining', pageInfo, userId,uniId)    
       # utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
    utils.logger(logger, correlationId, 'INFO', ('Total time in InvokeSimilarityAnalytics function which is for training is  %s seconds ---'%(time.time() - start_time)),str(uniId))
    
    return
    

def Invoke_train_Developer(correlationId,pageInfo,userId,uniId):
    try:
        logger = utils.logger('Get', correlationId)
        cpu,memory=utils.Memorycpu()
        utils.logger(logger, correlationId, 'INFO', ('import in tasks for Invoke_train_Developer function took '  + str(end-start) + ' secs'+ " with CPU: "+cpu+" Memory: "+memory),str(uniId))
        IngestData_json, selectedCols,allparams = utils.getRequestParams(correlationId,pageInfo,uniId)
        utils.updQdb(correlationId, 'P', '5', pageInfo, userId,uniId)
        try:
            IngestData_json = eval(IngestData_json)
        except:
            IngestData_json = json.loads(IngestData_json)
        utils.logger(logger, correlationId, 'INFO', str(IngestData_json),str(uniId))
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
        MappingColumns = {}
        if mapping_flag == 'False':
            utils.updQdb(correlationId, 'P', '10', pageInfo, userId,uniId)
            MultiSourceDfs,MappingColumns,lastDateDict,errFlag =  invokeIngestData.Read_Data(correlationId,pageInfo,userId,uniId,IngestData_json,True)
            temp = {}
            if MultiSourceDfs['Entity'] != {}:
                utils.logger(logger, correlationId, 'INFO', str(MultiSourceDfs['Entity'].keys()),str(uniId))
                msg = ""
                for k,v in MultiSourceDfs['Entity'].items():
                    utils.logger(logger, correlationId, 'INFO', str(type(v)))
                    utils.logger(logger, correlationId, 'INFO', str(k))
                    if  type(v) != str :
                        if k == "Codecommit":
                            temp["df2"] = v
                        else:
                            temp["df1"] = v
                    else:
                        msg = msg+""+str(k)+":"+v
                if msg!="":
                    if pageInfo == 'Retrain' or auto_retrain:
                        utils.logger(logger, correlationId, 'INFO','Message is'+ str(msg))
                        utils.updateModelStatus(correlationId,uniId,"","Completed","Retrain Failed"+ str(msg))
                    else:
                        utils.updateModelStatus(correlationId,uniId,"","Warning",msg)
                    utils.updQdb(correlationId, 'E', "ERROR", pageInfo, userId,uniId,msg)
                    utils.logger(logger, correlationId, 'INFO', str(auto_retrain))
                    if auto_retrain:
                        utils.logger(logger, correlationId, 'INFO', 'Line 292' +str(auto_retrain))
                        raise Exception(msg)
				
            j =  list(MultiSourceDfs['Entity'].keys())            
            for i in j:
                del MultiSourceDfs['Entity'][i]
            #print("temp[df2]['blockerissuecount']::::",temp["df2"]['blockerissuecount'].head())
            df = pd.merge(temp["df1"], temp["df2"], left_on="workitemexternalid",right_on="itemexternalid", how ='left')
            df.rename(columns = {'description_x':'Description'},inplace = True)
            MultiSourceDfs['File'] = {"File.csv":df}
            
            utils.updQdb(correlationId, 'P', '60', pageInfo, userId,uniId)
            
            message,data = utils.save_data_multi_file(correlationId, pageInfo, userId, uniId, MultiSourceDfs, parent, mapping,
                                                         mapping_flag, ClientUID, DeliveryConstructUId, insta_id, auto_retrain,
                                                         datapre=None, lastDateDict=lastDateDict,MappingColumns= MappingColumns,usecase = None)
            
            data = data[selectedCols]
            #print("selectedCols:::",selectedCols)
            #print("data::::",data['blockerissuecount'].head())
            try:
                utils.updQdb(correlationId, 'P', '70', pageInfo, userId,uniId)
                utils.updateModelStatus(correlationId,uniId,"","Progress","Model Training in progress")
                Message = BestDeveloper.model_training(data,correlationId,uniId)
                utils.updateModelStatus(correlationId,uniId,"","Completed","Model got created")
            except Exception as e:
                utils.logger(logger, correlationId, 'ERROR', 'Trace')
                utils.updateModelStatus(correlation_id,uniId,"","Error",str(e))
                
#            data = data_from_chunks(correlationId, "AIServiceIngestData")
            utils.logger(logger, correlationId, 'INFO', str('complete'),str(uniId))
            utils.updQdb(correlationId, 'C', '100', pageInfo, userId,uniId)
    except Exception as e:
        #utils.logger(logger, correlationId, 'ERROR', 'Trace')
        if str(e.args[0]).__contains__('Data is not available for your selection'):
            utils.logger(logger, correlationId, 'INFO',str("Data is not available for your selection"),str(uniId))
        else:
            utils.logger(logger, correlationId, 'ERROR', 'Trace')
    return

def  InvokePredictDeveloper(correlationId,pageInfo,userId,uniId,Params):

    logger = utils.logger('Get', correlationId)
    cpu,memory=utils.Memorycpu()
    utils.logger(logger, correlationId, 'INFO', ('import in tasks for Invoke_Predict_Developer function took '  + str(end-start) + ' secs'+ " with CPU: "+cpu+" Memory: "+memory),str(uniId))
    #print(correlationId,"inside.......................")
    try:
        
        ClientUID = Params['ClientId']
        Dcuid = Params['DeliveryConstructId']
        wuid =  Params['WorkItemExternalId']
        wtype =  Params['WorkItemType']

 #       test_data = utils.data_from_chunks(correlationId,"AIServiceIngestData")
#        test_data = test_data[test_data.workitemexternalid.isin([wuid])]
        
        test_data = utils.GetTestData(ClientUID,Dcuid,wtype,wuid)
        test_data = test_data[["Description","workitemexternalid"]]
        test_data.dropna(inplace = True)
        test_data = test_data.drop_duplicates(subset=["workitemexternalid"], keep="last")
        if not  test_data.empty:
            if test_data.Description.isnull().sum() < 1:
                prediction = BestDeveloper.BestDeveloperPrediction(test_data,correlationId,uniId)
            else:
                prediction = "0"
        else:
             prediction = "0"
    except Exception as e:
        prediction = False
        #utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
        utils.logger(logger, correlationId, 'ERROR', 'Trace')
        #print(str(e.args[0]))
    return prediction

def InvokeGetSummarization(correlationId,pageInfo,userId,uniId,summary_path):
    start_time = time.time()
    logger = utils.logger('Get', correlationId)
    cpu,memory=utils.Memorycpu()
    utils.logger(logger, correlationId, 'INFO', ('import in tasks for InvokeGetSummarization function took '  + str(end-start) + ' secs'+ " with CPU: "+cpu+" Memory: "+memory),str(uniId))
    try:
        utils.UpdateAIServicesPrediction(correlationId,uniId,"","P",5,"")
        args = utils.getRequestSummaryDetails(correlationId,uniId)
        
        minW = args["minrange"] ### changes
        maxW = args["maxrange"] ### changes
        text = args["query"] ### changes
        
        SummText = text_summarization.getSummarization(minW,maxW,text,correlationId,uniId,summary_path) ### changes
        utils.UpdateAIServicesPrediction(correlationId,uniId,SummText,"C",100,"TaskCompleted")
        end_time = time.time()
        utils.logger(logger, correlationId, 'INFO', ('Total time took for InvokeGetSummarization function in seconds is {}'.format(end_time-start_time)),str(uniId))
    except Exception as e:
        utils.UpdateAIServicesPrediction(correlationId,uniId,"","E",0,str(e.args[0]))
        utils.logger(logger, correlationId, 'ERROR', 'Trace',str(uniId))
    return 

def multiple_pred(correlationId,pageInfo,UniId,userId,results_count,similarity_type,params,logger):
    Pred=[] 
    Pred_status=[]
    try:
        for pred_data in params:
            result={}
            data=[]
            data_id=pred_data["id"]
            del pred_data["id"]
            #print(pred_data)
            data.append(pred_data)
            #print(data)
            svectors=spacy_vectors
            pred_sim=similarity_analytics.get_similarity(correlationId,pageInfo,UniId,data,results_count,similarity_type,svectors)
            #print(pred_sim)
            result["id"]=data_id
            result["prediction"]=json.dumps(pred_sim)
            Pred.append(result)
        import pandas as pd
        data = pd.DataFrame(Pred,index=None)

        data.columns = data.columns.astype(str)
        
        chunk,size = utils.files_split(data,Incremental = False,appname=None)
        utils.logger(logger,correlationId,'INFO',"Length of chunk is "+str(len(chunk)),str(UniId))

         
        utils.save_data_chunk_bulk(chunk, "AIServicesPrediction", correlationId,  UniId,userId, similarity_type,None,pageInfo = 'Prediction File', Incremental=False,requestId=None,sourceDetails=None, colunivals=None, timeseries=None, datapre=None, lastDateDict=None,previousLastDate = None,DataSetUId=None)
        utils.logger(logger,correlationId,'INFO',"Saved the chunks in DB",str(UniId))
        utils.logger(logger,correlationId,'INFO','Saved the data in chunks',str(UniId))

        count_pred=len(Pred)
        count_fail=0
        for item in Pred:
            if item['prediction']==False or item['prediction']=='False' or item['prediction']=='false':
                count_fail=count_fail+1
                item['prediction']="Unsuccessful Prediction for id :'"+item['id']+"'"
        count_sucess=count_pred-count_fail
        if count_sucess==count_pred:
             m = "Predictions Successful for "+str(count_sucess)+" record(s)"
             s = "True"
             P = True
        elif count_fail==count_pred:
            m = "Predictions Unsuccessful for "+str(count_fail)+" record(s)"
            s = "False"
            P = []
        else:
             m = "Predictions Successful for "+str(count_sucess)+" record(s) and Unsuccessful for "+str(count_fail)+" record(s)"
             s = "True"
             P = True
        Pred_status.append(m)
        Pred_status.append(s)
        Pred_status.append(P)
        #pred_queue.put({'Pred':Pred,'Pred_status':Pred_status})
        utils.updPredStatus(correlationId, 'C', '100%', 'Prediction Status', userId,UniId)
    
    except KeyError:
        utils.updPredStatus(correlationId, 'E', '', 'Prediction Status', userId,UniId)
        raise Exception("Error when fetching the 'id' from Data.Please provide 'id' in the data")  


def PredictIt():
    start_time=time.time()
    if request.method == 'POST':
        try:
            print("Inside the webAPI  New Predict It function")
            args = request.get_json()
            correlationId = args['CorrelationId']
            pageInfo      = args['PageInfo']
            userId        = args['UserId']  
            UniId         = args['UniqueId']
            params        = args['Params']
            results_count   = args['noOfResults']
            
            userId = utils.getUserId(correlationId,UniId)
            
            if userId==False:
                utils.updQdb(correlationId, 'E', "ERROR",'Trace',str(UniId))
                raise Exception("Error when fetching the User Id from DB")
            
            logger = utils.logger('Get', correlationId)
            utils.logger(logger, correlationId, 'INFO', 'In line 163,Args is '+str(args),str(UniId))

            print("Line 164")
            cpu,memory=utils.Memorycpu()
            utils.logger(logger, correlationId, 'INFO', ('import in webapi for Predictit func took '  + str(end-start) + ' secs'+ " with CPU: "+str(cpu)+" Memory: "+str(memory)),str(UniId))
                #newly added
                
                
            usecase_id = utils.getUseCaseId(correlationId,UniId)
            language=utils.GetLanguageForModel(correlationId)
            utils.logger(logger, correlationId, 'INFO',"language ::"+str(language) ,str(UniId))
            utils.logger(logger, correlationId, 'INFO',"spacy vectors before laoding ::"+str(spacy_vectors) ,str(UniId))

            load_model(language)
            
            utils.logger(logger, correlationId, 'INFO',"spacy vectors after laoding ::"+str(spacy_vectors) ,str(UniId))

            if str(usecase_id)!= config['TOUseCaseId']['DefectUseCaseId'] and str(usecase_id)!= config['TOUseCaseId']['TestCaseUseCaseId']:
                
                try:
                    similarity_type = args['Bulk']
                    print(args['Bulk'])
                except:
                    similarity_type = 'Single'
                
                print("Line 173")
                print(args,"Correlationid::::",correlationId,"pageInfo::::",pageInfo,"userId::::",userId,"UniId::::",UniId,"params::::",params,"Similarity Type:::",similarity_type)
                #print("Correlationid::::",correlationId,"pageInfo::::",pageInfo,"userId::::",userId,"UniId::::",UniId,"params::::",params)
                if pageInfo == "EvaluateSimilarityAanalytics":
                    if similarity_type=="Single":
                        utils.logger(logger, correlationId, 'INFO', 'Received single type in predictit',str(UniId))
                        Pred=similarity_analytics.get_similarity(correlationId,pageInfo,UniId,params,results_count,similarity_type,spacy_vectors)
                        
                    elif similarity_type=="Multiple":
                        utils.logger(logger, correlationId, 'INFO', 'Received Multiple type in predictit',str(UniId))
                        utils.updPredStatus(correlationId, 'P', '10%', 'Prediction Status', userId,UniId)
                        #queue1 = Queue()
                        thread = Thread(target=multiple_pred, args=(correlationId, pageInfo, UniId,userId,results_count, similarity_type,  params,logger))
                        thread.daemon = True
                        thread.start()
                        # que_out=queue1.get()
                        # Pred=que_out['Pred']
                        # pred_status=que_out['Pred_status']
                        # #print(Pred,pred_status)
                        # m=pred_status[0]
                        # s=pred_status[1]
                        # P=pred_status[2]
                        
                            
                    elif similarity_type=="Bulk":
                        utils.logger(logger, correlationId, 'INFO', 'Received bulk in predictit',str(UniId))
                        utils.updPredStatus(correlationId, 'P', '10%', 'Prediction Status', userId,UniId)
                        #queue2 = Queue()
                        thread1=Thread(target=similarity_analytics.get_bulk_similarity, args=(correlationId,pageInfo,UniId,userId,similarity_type))
                        thread1.daemon = True
                        thread1.start()
                        utils.logger(logger, correlationId, 'INFO', 'thhread in bulk'+str(thread1.is_alive()),str(UniId))
                        #Pred=queue2.get()
                        # if Pred==True:
                        #     utils.updPredStatus(correlationId,'C', '100%', 'Prediction Status', userId,UniId)
                        # else:
                        #     utils.updPredStatus(correlationId,'E', '', 'Prediction Status', userId,UniId)
                        
                
                elif pageInfo == "EvaluateNextWord":
                    Pred = Next_Word.PredictNextWord(correlationId,pageInfo,UniId,params)
                
                else:
                    pass
                        
                        
            else:
                
                print(args['Bulk'])
                similarity_type = args['Bulk']
                utils.updPredStatus(correlationId, 'P', '10%', 'Prediction Status', userId,UniId)

                utils.logger(logger, correlationId, 'INFO', 'Received bulk for Test Optimizer in predictit',str(UniId))
                
                #Pred = similarity_analytics.get_bulk_similarity(correlationId,pageInfo,UniId,userId,similarity_type)
                #queue3 = Queue()
                thread1=Thread(target=similarity_analytics.get_bulk_similarity, args=(correlationId,pageInfo,UniId,userId,similarity_type))
                thread1.daemon = True
                thread1.start()
                # Pred=queue3.get()
                # if Pred==True:
                #     utils.updPredStatus(correlationId,'C', '100%', 'Prediction Status', userId,UniId)
                # else:
                #     utils.updPredStatus(correlationId,'E', '', 'Prediction Status', userId,UniId)
                        
                #utils.logger(logger, correlationId, 'INFO', str(Pred),str(UniId))

                

            #print("Pred::::::::::::::::::",Pred)
                
            
            
                 
            if similarity_type=="Single":
                """code for arranging columns in a such way to get 'score' to last  """
                try:
                    import pandas as pd
                    data_pred_df=pd.DataFrame(Pred)
                    score_data=list(data_pred_df["score"])
                    data_pred_df1=data_pred_df.drop('score',axis=1)
                    data_pred_df1.insert(loc=0, column='score', value=score_data)
                    cols = list(data_pred_df1.columns)
                    cols.reverse()
                    data_pred_df2=data_pred_df1[cols]
                    data_pred_df_dict=data_pred_df2.to_dict(orient="records")
                    P = data_pred_df_dict
                except KeyError:
                    P=Pred
            
                m = "Predictions Successful"
                s = "True"
                                       
        except Exception as e:
            utils.updPredStatus(correlationId, 'E', '', 'Prediction Status', userId,UniId)                           
            utils.logger(logger, correlationId, 'ERROR', 'Trace',str(UniId))
            utils.logger(logger, correlationId, 'INFO', ('Total time in predictit function webapi is  %s seconds ---'%(time.time() - start_time)),str(UniId))
            return jsonify({'status': 'false', 'message':str(e.args)}), 200
        utils.logger(logger, correlationId, 'INFO', ('Total time in predictit function webapi is  %s seconds ---'%(time.time() - start_time)),str(UniId))
        if similarity_type=="Single":
            return jsonify({'status': s, 'message':m,"Predictions":P}), 200
        else:
            return jsonify({'message':"Backend process initiated for "+str(similarity_type)+" prediction","Predictions":True,'status': "True"}), 200
if __name__ == '__main__':  
    
    correlation_id     = sys.argv[1]
    pageInfo           = sys.argv[2]
    userId             = sys.argv[3]
    uniId              = sys.argv[4]
    flag               = sys.argv[5]
    try:
        if flag == "InvokeSimilarityAnalytics":
            InvokeSimilarityAnalytics(correlation_id,pageInfo,userId,uniId)
        elif flag == "traindeveloper":
            Invoke_train_Developer(correlation_id,pageInfo,userId,uniId)
        elif flag == "NextWord":
            InvokeSimilarityAnalytics(correlation_id,pageInfo,userId,uniId,"NextWord")
        elif flag == "InvokeSimilarityPrediction":
             InvokeSimilarityPrediction(correlation_id,pageInfo,userId,uniId)
        elif flag == "GetSummarization":
            summary_path=sys.argv[6]
            InvokeGetSummarization(correlation_id,pageInfo,userId,uniId,summary_path)
        else:
             pass
    except Exception as ex:
        print(str(ex))




