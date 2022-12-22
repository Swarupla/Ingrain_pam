# -*- coding: utf-8 -*-
"""
Created on Fri Jun  5 09:05:44 2020

@author: saurav.b.mondal
"""
from flask import Flask,request,jsonify,Response
import sys
import logging
# SSAI Scripts
from SSAIutils import utils
from flask import jsonify
from pandas import Timestamp
from numpy import nan
import datetime
from urllib.parse import unquote
import requests
from main import invokeIngestData_Clustering
from datapreprocessing import data_quality_check_Clustering
from datapreprocessing import Clustering_data_preprocessing
from Models import DBSCAN
from Models import Kmeans_Minibatch
from Models import MapData
import evaluation_api
from visualization import wordcloud_api
from visualization import viz
from main.Auth import validateToken,validateTokenUsingUrl
#from Authentication import Auth




app = Flask(__name__)
auth_type = utils.get_auth_type()


prefix="/clustering"
@app.route('/')
def index():
    return "Clustering Python API"

@app.route(prefix+"/hello", methods=['GET','POST'])
def test():
   print("hello my new message.")
   return "I am from Hello method"    

@app.route('/ModelTraining', methods=['POST'])
def uploadclusteringdata():
    logger = utils.logger('Get', 'auth')

    if(request.method == 'POST'):
        if request.method == "GET":
            return "Get method not allowed"
        if auth_type == 'AzureAD':
            token = request.headers['Authorization']
            #token = token.strip("Bearer").strip("bearer").strip()
            if validateToken(token) == 'Fail':
                utils.logger(logger, 'auth', 'ERROR','Trace')
                return Response(status=401)
        elif auth_type == 'WindowsAuthProvider':
            variable=True
        elif auth_type=='Forms' or auth_type=='Form':
            utils.logger(logger,'auth', 'INFO', "enter into form authentication"+str(request.headers))
            try:
                token = request.headers['Authorization']
            except Exception as e:
                utils.logger(logger,'auth', 'INFO', "Exception in form token"+str(e.args))
                utils.logger(logger, 'auth', 'ERROR','Trace')
    
            if validateTokenUsingUrl(token)=='Fail':
                return Response(status=401)
#        try:
#            token = request.headers['Authorization']
#            token = token.strip("Bearer").strip("bearer").strip()
            #print (token)
#            if Auth.validateToken(token) == 'Fail':
#               return Response(status=401)
#        except:
#            return Response(status=401)

        abc=request.get_json()
        print("Incoming request is : ",abc)
        correlationId=abc.get('CorrelationId')
        pageInfo=abc.get('pageInfo')
        userId=abc.get('UserId')
        UniId=abc.get('UniId')
        InvokeIngestDataflag=abc.get('IsDataUpload')   
        publish_usecase=abc.get('Publish_Case')
        
        dbconn,dbcollection=utils.open_dbconn('Clustering_IngestData')
        data_json = list(dbcollection.find({"CorrelationId": correlationId}))
        print(data_json)
        try:
            ClientID=data_json[0].get('ClientID')
        except:
            ClientID=None
        try:    
            DCUID=data_json[0].get('DCUID')
        except:
            DCUID=None
        try:
            ServiceID=data_json[0].get('ServiceID')
        except:
            ServiceID=None
        try:
            userdefinedmodelname=data_json[0].get('ModelName')
        except:
            userdefinedmodelname=None
        
        try:
            retrain=data_json[0].get('retrain')
        except:
            retrain=False
        
        try:
            EnDeRequired = data_json[0].get('DBEncryptionRequired')
        except:
            return "Encryption Flag is a mandatory field"
        
        dbconn.close()
        if pageInfo=="wordcloud":
            utils.insQdb(correlationId, 'P', '5', 'wordcloud', userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
            print("Invoking ingest data function from Data ingestion api file for wordcloud")
            
            try: 
                invokeIngestData_Clustering.main(correlationId,pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
            
                
                if publish_usecase:
                    subset=utils.subset_columns_clustering(correlationId,'Clustering_BusinessProblem')
                    if subset:
                       utils.updQdb(correlationId,'C',"100",pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
                    else:
                       utils.updQdb(correlationId,'E',"Invalid Columns. The selected columns do not exist in the Ingested Data",pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
                else:
                    utils.updQdb(correlationId,'C',"100",pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
            except Exception as e:
                utils.updQdb(correlationId,'E',e.args[0],pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
        else:
            if retrain==True:
                dbconn,dbcollection=utils.open_dbconn('Clustering_StatusTable')
                dbcollection.delete_many({"CorrelationId": correlationId})
                dbconn.close()
                dbconn,dbcollection=utils.open_dbconn('Clustering_DataPreprocessing')
                dbcollection.delete_many({"CorrelationId": correlationId})
                dbconn.close()
                dbconn,dbcollection=utils.open_dbconn('Clustering_ViewTrainedData')
                dbcollection.delete_many({"CorrelationId": correlationId})
                dbconn.close()
                dbconn,dbcollection=utils.open_dbconn('Clustering_ViewMappedData')
                dbcollection.delete_many({"CorrelationId": correlationId})
                dbconn.close()
             
            
            if (retrain!=True and InvokeIngestDataflag==True and publish_usecase!=True) or (retrain!=True and InvokeIngestDataflag==True and publish_usecase==True) :
                utils.insQdb(correlationId, 'P', '5', 'InvokeIngestData', userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
                print("Invoking ingest data function from Data ingestion api file")
                try: 
                    invokeIngestData_Clustering.main(correlationId,pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
                except Exception as e:
                    utils.updQdb(correlationId,'E',e.args[0],pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
                    return 'Invoke Ingest Data Failed'
                if publish_usecase!=True:
                    return "Invoke Ingest Data Successfully Invoked"

            if (retrain!=True and InvokeIngestDataflag!=True and publish_usecase!=True) or (retrain==True and InvokeIngestDataflag!=True and publish_usecase!=True) or (retrain!=True and InvokeIngestDataflag==True and publish_usecase==True) or (retrain==True and InvokeIngestDataflag!=True and publish_usecase==True):
                utils.insQdb(correlationId, 'P', '5', 'DataCuration', userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
                try: 
                     data_quality_check_Clustering.main(correlationId, 'DataCuration', userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname,EnDeRequired=EnDeRequired)
                except Exception as e:
                    utils.updQdb(correlationId,'E',e.args[0],pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
                    return 'Data Curation Failed'
                utils.insQdb(correlationId, 'P', '5', 'DataTransformation', userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
                try: 
                     Clustering_data_preprocessing.main(correlationId, 'DataTransformation', userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname,EnDeRequired=EnDeRequired)
                except Exception as e:
                    utils.updQdb(correlationId,'E',e.args[0],pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
                    return 'Data Transformation Failed'

            if data_json[0].get('SelectedModels').get('KMeans').get('Train_model')=='True':
                utils.insQdb(correlationId, 'P', '5', 'Model Training',userId,modelName='KMeans',problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
                try: 
                     Kmeans_Minibatch.main(correlationId,'KMeans',data_json[0].get('SelectedModels').get('KMeans').get('No_of_Clusters'),'Model Training',userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname,EnDeRequired=EnDeRequired)
                except Exception as e:
                    utils.updQdb(correlationId,'E',e.args[0],pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
                    return 'Model Training Failed'
            if data_json[0].get('SelectedModels').get('DBSCAN').get('Train_model')=='True':
                utils.insQdb(correlationId, 'P', '5', 'Model Training',userId,modelName='DBSCAN',problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
                try: 
                     DBSCAN.main(correlationId,'DBSCAN','Model Training',userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname,EnDeRequired=EnDeRequired)
                except Exception as e:
                    utils.updQdb(correlationId,'E',e.args[0],pageInfo,userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
                    return 'Model Training Failed'
                    
        return 'Success!'
                
@app.route('/EvalTraining', methods=['POST'])
def evaluateclusteringdata():
    logger = utils.logger('Get', 'auth')

    if(request.method == 'POST'):
        if request.method == "GET":
            return "Get method not allowed"
        if auth_type == 'AzureAD':
            token = request.headers['Authorization']
            #token = token.strip("Bearer").strip("bearer").strip()
            if validateToken(token) == 'Fail':
                utils.logger(logger, 'auth', 'ERROR','Trace')
                return Response(status=401)
        elif auth_type == 'WindowsAuthProvider':
            variable=True
        elif auth_type=='Forms' or auth_type=='Form':
            utils.logger(logger,'auth', 'INFO', "enter into form authentication"+str(request.headers))
            try:
                token = request.headers['Authorization']
            except Exception as e:
                utils.logger(logger,'auth', 'INFO', "Exception in form token"+str(e.args))
                utils.logger(logger, 'auth', 'ERROR','Trace')
    
            if validateTokenUsingUrl(token)=='Fail':
                return Response(status=401)
#        try:
#            token = request.headers['Authorization']
#            token = token.strip("Bearer").strip("bearer").strip()
#            #print (token)
#            if Auth.validateToken(token) == 'Fail':
#               return Response(status=401)
#        except:
#            return Response(status=401)

        abc=request.get_json()
        correlationId=abc.get('CorrelationId')
        #bulk=abc.get('bulk')
        Eval_Id=abc.get('UniId')
        
        dbconn,dbcollection=utils.open_dbconn('Clustering_IngestData')
        data_json = list(dbcollection.find({"CorrelationId": correlationId}))
        ClientID=data_json[0].get('ClientID')
        DCUID=data_json[0].get('DCUID')
        ServiceID=data_json[0].get('ServiceID')
        userdefinedmodelname=data_json[0].get('ModelName')
        col_selectedbyuser=list(data_json[0].get('Columnsselectedbyuser'))                                                                 
        try:
            EnDeRequired = data_json[0].get('DBEncryptionRequired')
        except:
            return "Encryption Flag is a mandatory field"
        dbconn.close()

        dbconn,dbcollection=utils.open_dbconn('Clustering_StatusTable')
        if dbcollection.count_documents({"CorrelationId": correlationId,"pageInfo":'Evaluate_API'}, limit = 1) != 0:
            dbcollection.delete_many({"CorrelationId": correlationId,"pageInfo":'Evaluate_API'})
            dbconn.close()
        else:
            dbconn.close()

        
        #accesstoken=request.headers['authorization']
        #creds=requests.post(tokendataurl,accesstoken)
        dbconn,dbcollection=utils.open_dbconn('Clustering_Eval')
        data_json = list(dbcollection.find({"CorrelationId": correlationId,"UniId":Eval_Id}))
        dbconn.close()
        
        print("JSON IS: ",data_json[0])
        pageInfo=data_json[0].get('PageInfo')
        userId=data_json[0].get('UserId')
        data_test=data_json[0].get('Data')
        for x,y in data_json[0].get('SelectedModels').items():
            if y.get('Train_model')=='True':
                modelName=x        
        mapping=data_json[0].get('mapping')
               
        if data_json[0].get('SelectedModels').get(modelName).get('Train_model')=='True':
            utils.insQdb(correlationId, 'P', '5', 'Evaluate_API',userId,modelName,problemType='Clustering',UniId=Eval_Id,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
            evaluation_api.main(correlationId,data_test,modelName,pageInfo,userId,Eval_Id,col_selectedbyuser,bulk=False,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname,mapping=mapping,EnDeRequired=EnDeRequired)
        

            
        #if type=='File':
                #data=pd.DataFrame(path)
                #utils.updQdb(5,correlationId,etc)
        print('Done')
        return ("API_Success!")
        

        
@app.route('/MapClusteringData', methods=['POST'])
def map_clustering_data():
    if(request.method == 'POST'):
        if request.method == "GET":
            return "Get method not allowed"
    
    json_data=request.get_json()
    correlationId=json_data.get('CorrelationId')
    pageInfo=json_data.get('pageInfo')
    userId=json_data.get('UserId')
    UniId=json_data.get('UniId')
    mapping=json_data.get('MappingData')
    modelName=json_data.get('modeltype')
    if modelName is None:
        return "Model Type cannot be Empty!"
    else:
        modelName=json_data.get('modeltype')
    
    dbconn,dbcollection=utils.open_dbconn('Clustering_IngestData')
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    print(data_json)
    try:
        ClientID=data_json[0].get('ClientID')
    except:
        ClientID=None
    try:    
        DCUID=data_json[0].get('DCUID')
    except:
        DCUID=None
    try:
        ServiceID=data_json[0].get('ServiceID')
    except:
        ServiceID=None
    try:
        userdefinedmodelname=data_json[0].get('ModelName')
    except:
        userdefinedmodelname=None
    dbconn.close()
    
    if modelName=='KMeans':
        utils.insQdb(correlationId, 'P', '5', pageInfo,userId,modelName=modelName,problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
        MapData.main(correlationId,pageInfo,userId,mapping,modelName=modelName,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
    elif modelName=='DBSCAN':
        utils.insQdb(correlationId, 'P', '5', pageInfo,userId,modelName=modelName,problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
        MapData.main(correlationId,pageInfo,userId,mapping,modelName=modelName,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
    return "Success"
    

@app.route('/wordcloud_generation', methods=['POST'])
def generate_wordcloud():
    logger = utils.logger('Get', 'auth')

    if(request.method == 'POST'):
        if request.method == "GET":
            return "Get method not allowed"
        try:
            if auth_type == 'AzureAD':
                token = request.headers['Authorization']
                #token = token.strip("Bearer").strip("bearer").strip()
                if validateToken(token) == 'Fail':
                    utils.logger(logger, 'auth', 'ERROR','Trace')
                    return Response(status=401)
            elif auth_type == 'WindowsAuthProvider':
                variable=True
            elif auth_type=='Forms' or auth_type=='Form':
                utils.logger(logger,'auth', 'INFO', "enter into form authentication"+str(request.headers))
                try:
                    token = request.headers['Authorization']
                except Exception as e:
                    utils.logger(logger,'auth', 'INFO', "Exception in form token"+str(e.args))
                    utils.logger(logger, 'auth', 'ERROR','Trace')
        
                if validateTokenUsingUrl(token)=='Fail':
                    return Response(status=401)
        except:
                response={}
                response["output"]=""
                response["message"]="Invalid token."
                return response
            
      
        #try:
        #    token = request.headers['Authorization']
        #    if validateToken(token) == 'Fail':
        #        return Response(status=401) 
        #except:
        #    response={}
        #    response["output"]=""
        #    response["message"]="Invalid token."
        #    return response
        json_data=request.get_json()
        #print(json_data)
        #print(json_data)
        correlationId=json_data.get('CorrelationId')
        pageInfo=json_data.get('pageInfo')
        stopword_list=json_data.get('stopword')
        #userId=json_data.get('UserId')
        try:
            max_words=json_data.get('max_words')
        except:
            max_words=1000
        
        response={}
        try:
            response=wordcloud_api.gen_wordcloud(correlationId,stopword_list,pageInfo,max_words)
            #print("Got the response----",img_str)
            
        except Exception as e:
            print(e)
            response["message"]="Internal server error"
            response["output"]=""
        
        return jsonify(response)



@app.route('/visualization', methods=['POST'])
def visualization():
    logger = utils.logger('Get', 'auth')

    if(request.method == 'POST'):
        if request.method == "GET":
            return "Get method not allowed"
        try:
            if auth_type == 'AzureAD':
                token = request.headers['Authorization']
                #token = token.strip("Bearer").strip("bearer").strip()
                if validateToken(token) == 'Fail':
                    utils.logger(logger, 'auth', 'ERROR','Trace')
                    return Response(status=401)
            elif auth_type == 'WindowsAuthProvider':
                variable=True
            elif auth_type=='Forms' or auth_type=='Form':
                utils.logger(logger,'auth', 'INFO', "enter into form authentication"+str(request.headers))
                try:
                    token = request.headers['Authorization']
                except Exception as e:
                    utils.logger(logger,'auth', 'INFO', "Exception in form token"+str(e.args))
                    utils.logger(logger, 'auth', 'ERROR','Trace')
        
                if validateTokenUsingUrl(token)=='Fail':
                    return Response(status=401)
        except:
                response={}
                response["output"]=""
                response["message"]="Invalid token."
                return response
      
#    try:
#        token = request.headers['Authorization']
#        if validateToken(token) == 'Fail':
#            return Response(status=401) 
#    except:
#        response={}
#        response["output"]=""
#        response["message"]="Invalid token."
#        return response
        json_data=request.get_json()
        correlationId=json_data.get('CorrelationId')
        pageInfo=json_data.get('pageInfo')
        UniId=json_data.get('UniId')
        userId=json_data.get('UserId')
        modelName=json_data.get('SelectedModel')
    
        dbconn,dbcollection=utils.open_dbconn('Clustering_IngestData')
        data_json = list(dbcollection.find({"CorrelationId": correlationId}))
        print(data_json)
        try:
                ClientID=data_json[0].get('ClientID')
        except:
                ClientID=None
        try:    
                DCUID=data_json[0].get('DCUID')
        except:
                DCUID=None
        try:
                ServiceID=data_json[0].get('ServiceID')
        except:
                ServiceID=None
        try:
                userdefinedmodelname=data_json[0].get('ModelName')
        except:
                userdefinedmodelname=None    
        try:
            EnDeRequired = data_json[0].get('DBEncryptionRequired')
        except:
            return "Encryption Flag is a mandatory field"
    
        try:
            mapping=data_json[0].get('mapping')
            if modelName=='DBSCAN':
                mapping.update({"Cluster -1":"Noise Point"})
            keys=[eval(i.split(' ')[1]) for i in list(mapping.keys())]
            #keys.remove(max(keys))
        except:
            mapping=None
        
        trained_models=data_json[0].get('SelectedModels')  
        trained_model_list=[]
        for x,y in trained_models.items():
            if trained_models[x].get('Train_model')=='True':
                trained_model_list.append(x)    
        dbconn.close()
        if modelName not in trained_model_list:
            return 'Untrained Model Name has been sent for Visualization!'
    
        if data_json[0].get('ProblemType').get('Clustering').get('Text')=='True':
            flag=True
            problemtype='Text'
        elif data_json[0].get('ProblemType').get('Clustering').get('Non-Text')=='True':
            flag=False
            problemtype='Non-Text'
    
        utils.insQdb(correlationId, 'P', '5', pageInfo,userId,modelName=modelName,problemType='Clustering',UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
        message=viz.main(correlationId,keys,mapping,modelName,userId,pageInfo,problemtype=problemtype,flag=flag,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname,EnDeRequired=EnDeRequired)
    
        if message=='Success':
            return "Sucess!"
        else:    
            return "Internal Server Error!"

if __name__ == "__main__":
    app.run(host='0.0.0.0',port=5000)


#app.run(debug=True)
