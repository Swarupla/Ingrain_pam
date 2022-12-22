# -*- coding: utf-8 -*-
"""
Created on Mon Jun  1 16:10:33 2020

@author: a.gaffar
"""
from pymongo import MongoClient
from pymongo.errors import ServerSelectionTimeoutError
from pymongo.errors import NetworkTimeout
import platform



import configparser, os
mainPath = os.getcwd() 
import sys

sys.path.insert(0, mainPath)
import file_encryptor
config = configparser.RawConfigParser()

configpath = "./pythonconfig.ini"
try:
        config.read(configpath)
except UnicodeDecodeError:        
        config = file_encryptor.get_configparser_obj(configpath)

def open_dbconn(collection):
    if config['DBconnection']['ssl'] == 'False':
        connection = MongoClient(config['DBconnection']['connectionString'])
    else:
        connection = MongoClient(config['DBconnection']['connectionString'],
                             ssl_pem_passphrase=config['DBconnection']['certificatePassKey'],
                             ssl_certfile=config['DBconnection']['certificatePath'],
                             ssl_ca_certs=config['DBconnection']['certificateCAPath'])
        
    

    db = connection.get_default_database()   
    db_collection=db[collection]

    return connection,db_collection


def updateModelStatus(correlationid,modelname,modelstatus,status_msg):
    
    dbconn, dbcollection = open_dbconn('AICoreModels')
    
    if(modelstatus == "Error"):
        model_json = list(dbcollection.find({"CorrelationId": correlationid}))
        model_path = model_json[0]["PythonModelName"]
        print(model_path)
        if(model_path == "" or model_path == None):
            dbcollection.update_many({"CorrelationId": correlationid},{'$set':{"PythonModelName":modelname,
                                                                       "ModelStatus":modelstatus,
                                                                       "StatusMessage":status_msg}})
        else:
            dbcollection.update_many({"CorrelationId": correlationid},{'$set':{"PythonModelName":model_path,
                                                                       "ModelStatus":"ReTrain Failed",
                                                                       "StatusMessage":status_msg}})
    else:
        dbcollection.update_many({"CorrelationId": correlationid},{'$set':{"PythonModelName":modelname,
                                                                       "ModelStatus":modelstatus,
                                                                       "StatusMessage":status_msg}})

