# -*- coding: utf-8 -*-
"""
Created on Sat Aug  1 12:43:37 2020

@author: saurav.b.mondal- yes
"""
import time
start = time.time()
import platform
import psutil
cpu=str(psutil.cpu_percent())
memory=str(psutil.virtual_memory()[2])

if platform.system() == 'Linux':
    conf_path = '/IngrAIn_Python/main/pythonconfig.ini'
    work_dir = '/IngrAIn_Python'
elif platform.system() == 'Windows':
    conf_path = '\IngrAIn_Python\main\pythonconfig.ini'
    work_dir = '\IngrAIn_Python'

import configparser, os
mainPath = os.getcwd() + work_dir
import sys 

sys.path.insert(0, mainPath)
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
#from SSAIutils import encryption
import sys

import os

from datapreprocessing import Add_Features
from dataqualitychecks import data_quality_check
import warnings
from datetime import datetime
from pandas import Timestamp

end=time.time()
'''
correlationId = "758c1161-8449-4391-bd76-f1b8fbf8d0d8"
requestId = "ca71c177-5ff7-4fc7-9f22-ac1708639501"
pageInfo = "AddFeature"
userId = "saurav"
'''

correlationId = sys.argv[1]
requestId = sys.argv[2]
pageInfo = sys.argv[3]
userId = sys.argv[4]

try:
    utils.updQdb(correlationId, 'P', '5', pageInfo, userId)
    logger = utils.logger('Get', correlationId)
    utils.logger(logger, correlationId, 'INFO', ('import took '  + str(end-start)+ ' secs'+ " with CPU: "+cpu+" Memory "+memory),str(requestId))
    utils.logger(logger, correlationId, 'INFO', ('Add New features Started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
    invokeAddFeature, _, _ = utils.getRequestParams(correlationId, requestId)
    print(correlationId, requestId, pageInfo, userId)
    

    #invokeAddFeature = eval(invokeAddFeature)
    Add_Features.main(invokeAddFeature,correlationId,pageInfo, userId)
    print("Invoking datacleanup")
    data_quality_check.main(correlationId,"DataCleanUpAddFeature",userId)
    utils.updQdb(correlationId,'C','100',pageInfo,userId) 
    utils.logger(logger, correlationId, 'INFO', ('Add New features Completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))

except Exception as e:
    utils.updQdb(correlationId, 'E', e.args[0], pageInfo, userId)
    utils.logger(logger, correlationId, 'ERROR', 'Trace',str(requestId))
    utils.save_Py_Logs(logger, correlationId)
    sys.exit()
