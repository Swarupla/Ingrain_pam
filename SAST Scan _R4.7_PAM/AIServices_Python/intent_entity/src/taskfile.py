# -*- coding: utf-8 -*-
"""
Created on Fri Jun  5 12:47:08 2020

@author: a.gaffar
"""

import os,sys
import time
from multiprocessing import Process
    
from rasa.train import train_nlu,train
import os,sys
from rasa.model import unpack_model
from pathlib import Path
#sys.path.insert(0, "/var/www/myWizard.IngrAInAIServices.WebAPI.Python/")
#from DBProvider import updateModelStatus

mainDirectory = str(Path(__file__).parent.parent.parent.absolute())
print("Main directory path - "+mainDirectory)
sys.path.insert(0, mainDirectory)
from encrypt_decrypt_pickle import load_key,encrypt
from DBProvider import updateModelStatus

from shared.logger_helper import GenerateLogger
log_generator = GenerateLogger()
currentDirectory_logger = str(Path(__file__).parent.parent.absolute())
logger = log_generator.generate_logger(currentDirectory_logger + "/intent_entity_taskfile.log")
import glob
from cryptography.fernet import Fernet

def intent_mdl(config_path,nlu_data, output,correlation_id):
    response = {}

    try:
        print("Inside Intent Model Function")
        train_path=None
        fixed_model_name=None
        persist_nlu_training_data=True
        train_resp = train_nlu(config_path,nlu_data, output, train_path,fixed_model_name,persist_nlu_training_data)       
        print("Inside Intent Model Function1")
 
        file_basename = os.path.basename(train_resp)
        filename_without_extension = file_basename.split('.')[0]
        print('train_resp',train_resp)
        resp = unpack_model(train_resp,output+"/"+filename_without_extension)
        print(output+"/"+filename_without_extension)
        key = load_key()
        files = [f for f in glob.glob(output+"/"+filename_without_extension + "/nlu/*", recursive=True)]
        for f in files:
            print(f)
            encrypt(f, key)
        files1 = [f for f in glob.glob(output+"/"+filename_without_extension + "/*.json", recursive=True)]
        for f in files1:
            print(f)
            encrypt(f, key)
        if (train_resp!=None):
            response["message"] = ""
            response["result"] = filename_without_extension
            response["is_success"] = True
            print("successfully completed training")
            updateModelStatus(correlation_id,filename_without_extension,"Completed","Model got created")
            logger.info("successfully completed training")

        else:
            response["message"] = 'Unexpected Exception, Contact Administrator'
            response["result"] = ""
            response["is_success"] = False
            print("training response none")
            updateModelStatus(correlation_id,"","Error","Error in training")
            logger.info("training response none")

    except Exception as e:
        response["message"] = 'System has encountered an error.'
        response["is_success"] = "false"
        print("Exception: ",str(e))
        updateModelStatus(correlation_id,"","Error","Error in training")
        logger.error(str(e))


if __name__ == '__main__':  
    config_path=sys.argv[1]
    nlu_data=sys.argv[2]
    output=sys.argv[3]
    correlation_id=sys.argv[4]

    try:
       intent_mdl(config_path,nlu_data, output,correlation_id)
    except Exception as ex:
       logger.error(str(ex))

    