import time
start= time.time()
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
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
config = configparser.RawConfigParser()
configpath = str(os.getcwd()) + str(conf_path)
try:
        config.read(configpath)
except UnicodeDecodeError:
        config = file_encryptor.get_configparser_obj(configpath)

from SSAIutils import utils
#import pandas as pd
#import datetime
#import json
#import requests
#import configparser
from dataqualitychecks import data_quality_check
from datetime import datetime
from pandas import Timestamp
correlationId = sys.argv[1]
requestId = sys.argv[2]
pageInfo = sys.argv[3]
userId = sys.argv[4]
print (correlationId,requestId,pageInfo,userId)

logger = utils.logger('Get',correlationId)
count = utils.check_CorId("SSAI_IngrainRequests",correlationId,pageInfo)
print (count)
end = time.time()
if len(sys.argv) == 5:
    genericflow = False
elif len(sys.argv) > 5 and sys.argv[5] == 'True':
    genericflow = True
else:
    genericflow = False	
if count >= 1 and not genericflow:
   utils.logger(logger,correlationId,'WARNING',('DataCleanUp Request for correlation Id :'+str(correlationId)+ "is in Progress"),str(None))
   sys.exit()

utils.updQdb(correlationId,'P','5',pageInfo,userId)

#argParam,_ = eval(utils.getRequestParams(correlationId,requestId))

def dataCleanup(correlationId,pageInfo,userId,flag):
   try:  
        utils.logger(logger, correlationId, 'INFO', ('invoke datacleanup started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
        utils.updQdb(correlationId,'P','10',pageInfo,userId)
        if flag == 1: 
            data_quality_check.UpdateData(correlationId,pageInfo,userId,flag)
            
        elif flag == 2:
            data_quality_check.ViewDataQuality(correlationId,pageInfo,userId,flag)
        else:
            data_quality_check.main(correlationId,pageInfo,userId)
        utils.logger(logger, correlationId, 'INFO', ('invoke Completed started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))

   except Exception as e:
        utils.logger(logger,correlationId,'ERROR','Trace',str(requestId))
        utils.save_Py_Logs(logger,correlationId)
        sys.exit()
        #return str(e.args) 

def dataCleanup_for_single_file(correlationId,requestId,pageInfo,userId,flag):
    utils.logger(logger,correlationId,'INFO',('import took ' +str(end-start)+' secs'+ " with CPU: "+cpu +" Memory: "+memory),str(requestId))
    utils.logger(logger,correlationId,'INFO',
                 ('DataCleanUp : CorrelationID :'+str(correlationId)+
                   ' pageInfo :'+pageInfo+
                   ' UserId :'+userId),str(requestId))
    utils.updQdb(correlationId,'P','10',pageInfo,userId)
    data_quality_check.main(correlationId,pageInfo,userId,flag)
    return

try:

    utils.logger(logger,correlationId,'INFO',('import took ' +str(end-start)+' secs'+ " with CPU: "+cpu +" Memory: "+memory),str(requestId))
    utils.logger(logger,correlationId,'INFO',
                 ('DataCleanUp : CorrelationID :'+str(correlationId)+
                   ' pageInfo :'+pageInfo+
                   ' UserId :'+userId),str(requestId))
    
    
          #return jsonify({'status': 'false','message':"Request is in Progress"}), 200 
      
    if pageInfo == "DataCleanUp":
        flag  = 0
    elif pageInfo == "ViewDataQuality":
        flag  = 2
    else:
        flag  = 1
   
    #utils.insQdb(correlationId,'P','Task Scheduled',pageInfo,userId)
    if len(sys.argv) == 5:
        genericflow = False
    elif len(sys.argv) > 5 and sys.argv[5] == 'True':
        genericflow = True
    else:
        genericflow = False	
    print("genericflow",genericflow)
    if not genericflow:
        dataCleanup(correlationId,pageInfo,userId,flag)
    else:
        dataCleanup_for_single_file(correlationId,requestId,pageInfo,userId,'AutoTrain')
  
                                      
except Exception as e:
    utils.logger(logger,correlationId,'ERROR','Trace',str(requestId))
    utils.save_Py_Logs(logger,correlationId)
    sys.exit()
    #return jsonify({'status': 'false', 'message':str(e.args)}), 200
#else:
    #utils.logger(logger,correlationId,'INFO','Task scheduled',str(requestId))
    #utils.save_Py_Logs(logger,correlationId)
    #sys.exit()
    #return jsonify({'status': 'true','message':"Success.."}), 200
