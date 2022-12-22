# -*- coding: utf-8 -*-
"""
Created on Wed Feb  5 09:03:49 2020

@author: s.siddappa.dinnimani
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
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import configparser,os
mainPath =os.getcwd()+work_dir
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
sys.path.insert(0,mainPath)
import file_encryptor
config = configparser.RawConfigParser()
configpath = str(os.getcwd()) + str(conf_path)
try:
        config.read(configpath)
except UnicodeDecodeError:
        config = file_encryptor.get_configparser_obj(configpath)




from SSAIutils import utils
from Prescriptive_Analysis import PrescriptiveAnalysis
from datetime import datetime
from pandas import Timestamp


correlationId = sys.argv[1]
requestId = sys.argv[2]
pageInfo = sys.argv[3]
userId = sys.argv[4]
end = time.time()
def Prescriptive_Analysis(correlationId,wfId,requestId,pageInfo,userId,desiredValue):  
     try:  
        logger = utils.logger('Get',correlationId)
        utils.updQdb(correlationId,'P','5',pageInfo,userId,UniId=wfId)   
        PrescriptiveAnalysis.main(correlationId,wfId,requestId,pageInfo,userId,desiredValue)        
     except Exception as e:
        utils.logger(logger,correlationId,'ERROR','Trace',str(requestId))
        utils.updQdb(correlationId,'E',str(e),pageInfo,userId,UniId=wfId)    
        utils.save_Py_Logs(logger,correlationId)
        sys.exit()
        #return str(e.args)  

try:
    argParam,model,_ = utils.getRequestParams(correlationId,requestId)
    argParam = eval(argParam)

    wfId = argParam['WfId']
    desiredValue = argParam['desired_value']

    logger = utils.logger('Get',correlationId)
    utils.logger(logger, correlationId, 'INFO', ('import took '  +str(end-start)+' secs'+" with CPU: " +cpu+" Memory: "+memory),str(requestId))
              
    utils.logger(logger,correlationId,'INFO',
                             ('IngestData: '+
                              'CorrelationID :'+str(correlationId)+
                              ' WFId :'+str(wfId)+
                              ' Model :'+str(model)+
                              ' pageInfo :'+pageInfo+
                              ' UserId :'+userId
                              )) 
    utils.updQdb(correlationId,'P','Task Scheduled',pageInfo,userId,UniId = wfId)     
    utils.logger(logger, correlationId, 'INFO', ('Prescriptive analysis started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
    Prescriptive_Analysis(correlationId,wfId,requestId,pageInfo,userId,desiredValue)  
    utils.logger(logger, correlationId, 'INFO', ('Prescriptive analysis Completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))

except Exception as e:        
        utils.logger(logger,correlationId,'ERROR','Trace',str(requestId))   
        utils.save_Py_Logs(logger,correlationId)
        sys.exit()
        #return jsonify({'status': 'false', 'message':str(e.args)}), 200
else:
    utils.logger(logger,correlationId,'INFO','Task scheduled')
    utils.save_Py_Logs(logger,correlationId)
    sys.exit()
    #return jsonify({'status': 'true','message':"Success.."}), 200
