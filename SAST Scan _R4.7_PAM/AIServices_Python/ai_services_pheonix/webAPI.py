# -*- coding: utf-8 -*-
"""
Created on Fri Jul 24 14:41:38 2020

@author: s.siddappa.dinnimani
"""


import time
start=time.time()
#!flask/bin/python
from flask import Flask,request,jsonify
import subprocess 
import configparser, os
import utils
import tasks
from pathlib import Path
import similarity_analytics
#import Next_Word
import platform
if platform.system() == 'Linux':
    pass
elif platform.system() == 'Windows':
    from subprocess import check_output, PIPE

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
    if config['config']['path']=='/app/':
        pythonPath="python"
    else:
        pythonPath = mainDirectory+"/IngrAInAIServices_env/bin/python"
elif platform.system() == 'Windows':
    pythonPath = mainDirectory+"\\venv\\Scripts\\python"
import spacy
global spacy_vectors
spacy_vectors = spacy.load('en_core_web_lg', disable=['tagger', 'parser','ner','lemmatizer'])
import json
from threading import Thread
from queue import Queue
end=time.time()


def SimilarityAnalytics():
    start_time=time.time()
    try:          
        if request.method == 'GET':
            
            correlationId = request.args.get('correlationId')
            pageInfo      = request.args.get('pageInfo')
            userId        = request.args.get('userId')  
            UniId         = request.args.get('UniqueId')
            logger = utils.logger('Get', correlationId)
            cpu,memory=utils.Memorycpu()

            userId = utils.getUserId(correlationId,UniId)
            
            if userId==False:
                utils.updQdb(correlationId, 'E', "ERROR",'Trace',str(UniId))
                raise Exception("Error when fetching the User Id from DB")
            
            utils.logger(logger, correlationId, 'INFO', ('import in webapi for similarityanaltics func took '  + str(end-start) + ' secs'+ " with CPU: "+cpu+" Memory: "+memory),str(UniId))
            args       = correlationId+" "+pageInfo+" "+userId+" "+UniId+" "+"InvokeSimilarityAnalytics"
            pyFileName = mainDirectory+"/ai_services_pheonix/tasks.py"
            cmd        = pythonPath+" "+pyFileName+" "+args
                
            if platform.system() == 'Linux':
                subprocess.Popen(cmd,close_fds=True,shell=True)
                utils.logger(logger, correlationId, 'INFO', ('subprocess will commence for cmd {} through tasks.py'.format(cmd)),str(UniId))
            elif platform.system() == 'Windows':
                ps=subprocess.Popen(cmd,close_fds=True,shell=True,stdout=PIPE)
                utils.logger(logger, correlationId, 'INFO', ('subprocess will commence for cmd {} through tasks.py'.format(cmd)),str(UniId))
    except Exception as e:
        utils.logger(logger, correlationId, 'INFO', ('Total time in SimilarityAnalytics function webapi is  %s seconds ---'%(time.time() - start_time)),str(UniId))
        return jsonify({'status': 'false', 'message':str(e.args)}), 200
    utils.logger(logger, correlationId, 'INFO', ('Total time in SimilarityAnalytics function webapi is for training is  %s seconds ---'%(time.time() - start_time)),str(UniId))
    return jsonify({'status': 'True', 'message':"Success"}), 200


def NextWord():
     try:          
        if request.method == 'GET':
            
            correlationId = request.args.get('correlationId')
            pageInfo      = request.args.get('pageInfo')
            userId        = request.args.get('userId')  
            UniId         = request.args.get('UniqueId')
            
            userId = utils.getUserId(correlationId,UniId)
            
            if userId==False:
                utils.updQdb(correlationId, 'E', "ERROR",'Trace',str(UniId))
                raise Exception("Error when fetching the User Id from DB")
            
            
            args       = correlationId+" "+pageInfo+" "+userId+" "+UniId+" "+"NextWord"
            pyFileName = mainDirectory+"/ai_services_pheonix/tasks.py"
            cmd        = pythonPath+" "+pyFileName+" "+args
            subprocess.Popen(cmd,close_fds=True,shell=True)   

     except Exception as e:
        return jsonify({'status': 'false', 'message':str(e.args)}), 200
     return jsonify({'status': 'True', 'message':"Success"}), 200


def train_Developer():
     start_time=time.time()
     try:          
        if request.method == 'GET':
            correlationId = request.args.get('correlationId')
            pageInfo      = request.args.get('pageInfo')
            userId        = request.args.get('userId')  
            UniId         = request.args.get('UniqueId')
            
            userId = utils.getUserId(correlationId,UniId)
            
            if userId==False:
                utils.updQdb(correlationId, 'E', "ERROR",'Trace',str(UniId))
                raise Exception("Error when fetching the User Id from DB")
            
            logger = utils.logger('Get', correlationId)
            cpu,memory=utils.Memorycpu()
            utils.logger(logger, correlationId, 'INFO', ('import in webapi for traindeveloper func took '  + str(end-start) + ' secs'+ " with CPU: "+cpu+" Memory: "+memory),str(UniId))
            
            args       = correlationId+" "+pageInfo+" "+userId+" "+UniId+" "+"traindeveloper"
            pyFileName = mainDirectory+"/ai_services_pheonix/tasks.py"
            cmd        = pythonPath+" "+pyFileName+" "+args
            subprocess.Popen(cmd,close_fds=True,shell=True)
            
     except Exception as e:
        utils.logger(logger, correlationId, 'INFO', ('Total time in traindeveloper function webapi is  %s seconds ---'%(time.time() - start_time)),str(UniId))
        return jsonify({'status': 'false', 'message':str(e.args)}), 200
     utils.logger(logger, correlationId, 'INFO', ('Total time in traindeveloper function webapi is  %s seconds ---'%(time.time() - start_time)),str(UniId))
     return jsonify({'status': 'True', 'message':"Success"}), 200




def PredictDeveloper():
     start_time=time.time()
     try:          
        if request.method == 'POST':
            args = request.get_json()
            correlationId = args['CorrelationId']
            pageInfo      = args['PageInfo']
            userId        = args['UserId']  
            UniId         = args['UniqueId']
            params        = eval(args['Params'])
            
            userId = utils.getUserId(correlationId,UniId)
            
            if userId==False:
                utils.updQdb(correlationId, 'E', "ERROR",'Trace',str(UniId))
                raise Exception("Error when fetching the User Id from DB")
            
            logger = utils.logger('Get', correlationId)
            cpu,memory=utils.Memorycpu()
            utils.logger(logger, correlationId, 'INFO', ('import in webapi for predictdeveloper func took '  + str(end-start) + ' secs'+ " with CPU: "+cpu+" Memory: "+memory),str(UniId))
            p = tasks.InvokePredictDeveloper(correlationId,pageInfo,userId,UniId,params)
            if p == "0":
                m = "Description is Empty for your work-item, Please check the data"
                s = "False"
                p = []
            elif p == False:
                m = "Error During predictions"
                s = "False"
                p = []
            else:
                m  = "Predictions are successful"
                s = "True"
     except Exception as e:
        utils.logger(logger, correlationId, 'INFO', ('Total time in predictdeveloper function webapi is  %s seconds ---'%(time.time() - start_time)),str(UniId))
        return jsonify({'status': 'false', 'message':str(e.args)}), 200
     utils.logger(logger, correlationId, 'INFO', ('Total time in predictdeveloper function webapi is  %s seconds ---'%(time.time() - start_time)),str(UniId))
     return jsonify({'status': s, 'message':m,"Predictions":p}), 200


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
                        Pred =[]
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
                        except KeyError:
                            raise Exception("Error when fetching the 'id' from Data.Please provide 'id' in the data")
                    elif similarity_type=="Bulk":
                        utils.logger(logger, correlationId, 'INFO', 'Received bulk in predictit',str(UniId))
                        Pred = similarity_analytics.get_bulk_similarity(correlationId,pageInfo,UniId,userId,similarity_type)
                
                elif pageInfo == "EvaluateNextWord":
                    Pred = Next_Word.PredictNextWord(correlationId,pageInfo,UniId,params)
                
                else:
                    pass
                        
                        
            else:
                
                print(args['Bulk'])
                similarity_type = args['Bulk']
                utils.updPredStatus(correlationId, 'P', '10%', 'Prediction Status', userId,UniId)

                utils.logger(logger, correlationId, 'INFO', 'Received bulk for Test Optimizer in predictit',str(UniId))
                Pred = similarity_analytics.get_bulk_similarity(correlationId,pageInfo,UniId,userId,similarity_type)
                utils.logger(logger, correlationId, 'INFO', str(Pred),str(UniId))

                

            print("Pred::::::::::::::::::",Pred)
                
            
            
            if similarity_type=="Multiple":
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
                     P = Pred
                elif count_fail==count_pred:
                    m = "Predictions Unsuccessful for "+str(count_fail)+" record(s)"
                    s = "False"
                    P = []
                else:
                     m = "Predictions Successful for "+str(count_sucess)+" record(s) and Unsuccessful for "+str(count_fail)+" record(s)"
                     s = "True"
                     P = Pred
                    
            else: 
                if Pred == False:
                    m = "Predictions Unsuccessful"
                    s = "False"
                    P = []
                else:
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
                    else:
                        P = Pred
                    m = "Predictions Successful"
                    s = "True"
                                       
        except Exception as e:
            utils.logger(logger, correlationId, 'ERROR', 'Trace',str(UniId))
            utils.logger(logger, correlationId, 'INFO', ('Total time in predictit function webapi is  %s seconds ---'%(time.time() - start_time)),str(UniId))
            return jsonify({'status': 'false', 'message':str(e.args)}), 200
        utils.logger(logger, correlationId, 'INFO', ('Total time in predictit function webapi is  %s seconds ---'%(time.time() - start_time)),str(UniId))
        return jsonify({'status': s, 'message':m,"Predictions":P}), 200

def SimilarityPrediction(): #added function for handling similarity analytics multi value
    try:
        print('inside SimilarityPrediction')       
        if request.method == 'GET':
            return jsonify({'status': 'True', 'message':"Method doesnt exist"}), 500
    except Exception as e:
        print('Exception from similarity webapi',str(e))
        return jsonify({'status': 'false', 'message':str(e.args)}), 200
    return jsonify({'status': 'True', 'message':"Success"}), 200



def get_summary():
    start_time=time.time()
    print("REQUEST Recieced........................................................")       
    if request.method == 'POST':
        try:
            args = request.get_json()
            print("Response:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::",args)
            correlationId = args['CorrelationId']
            pageInfo      = args['PageInfo']
            userId        = args['UserId']
            UniId         = args['UniqueId']
            
            userId = utils.getUserIdTextSummary(correlationId,UniId)
            
            if userId==False:
                utils.updQdb(correlationId, 'E', "ERROR",'Trace',str(UniId))
                raise Exception("Error when fetching the User Id from DB")
            
            logger = utils.logger('Get', correlationId)
            
            cpu,memory=utils.Memorycpu()
            utils.logger(logger, correlationId, 'INFO', ('import in webapi for getsummary request func took '  + str(end-start) + ' secs'+ " with CPU: "+cpu+" Memory: "+memory),str(UniId))
            args       = correlationId+" "+pageInfo+" "+userId+" "+UniId+" "+"GetSummarization"+" "+summary_path
            pyFileName = mainDirectory+"/ai_services_pheonix/tasks.py"
            cmd        = pythonPath+" "+pyFileName+" "+args
            print(cmd)
            if platform.system() == 'Windows':
                cmd1 = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
                out, err = cmd1.communicate()
            else:
                subprocess.Popen(cmd,close_fds=True,shell=True)
            
        except Exception as e:
            utils.logger(logger, correlationId, 'ERROR', 'Trace',str(UniId))
            utils.logger(logger, correlationId, 'INFO', ('Total time in get_summary function webapi is  %s seconds ---'%(time.time() - start_time)),str(UniId))
            return jsonify({'status': 'False', 'message':str(e.args)}), 200
    utils.logger(logger, correlationId, 'INFO', ('Total time in get_summary function webapi is  %s seconds ---'%(time.time() - start_time)),str(UniId))
    return jsonify({'status': 'True', 'message':"Success"}), 200




















