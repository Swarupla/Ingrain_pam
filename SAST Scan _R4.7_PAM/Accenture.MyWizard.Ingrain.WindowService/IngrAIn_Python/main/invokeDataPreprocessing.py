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

from SSAIutils import utils
from datapreprocessing import Data_PreProcessing
from datetime import datetime
from pandas import Timestamp

correlationId = sys.argv[1]
requestId = sys.argv[2]
pageInfo = sys.argv[3]
userId = sys.argv[4]
utils.updQdb(correlationId,'P','Task Scheduled',pageInfo,userId)
print (correlationId,requestId,pageInfo,userId)
end = time.time()

def invokedatapreprocessing(correlationId,pageInfo,userId):
    try:  
        logger = utils.logger('Get',correlationId)
        utils.updQdb(correlationId,'P','10',pageInfo,userId)        
        Data_PreProcessing.main(correlationId,pageInfo,userId)
    except Exception as e:
        utils.logger(logger,correlationId,'ERROR','Trace',str(requestId))
        utils.save_Py_Logs(logger,correlationId)
        sys.exit()
#        return str(e.args)
    
try:
    logger = utils.logger('Get',correlationId)
    utils.logger(logger, correlationId, 'INFO', ('import took '  + str(end-start)+' secs'+" with CPU: "+cpu+" Memory: "+memory),str(requestId))
    utils.logger(logger,correlationId,'INFO',
                 ('DataCleanUp: Arguments '+str(sys.argv)+
                   ' CorrelationID :'+str(correlationId)+
                   ' pageInfo :'+pageInfo+
                  ' UserId :'+userId),str(requestId))            
    utils.logger(logger, correlationId, 'INFO', ('Datapreprocessing started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
               
    ##count = utils.check_CorId("SSAI_IngrainRequests",correlationId,pageInfo)
    ##if count >= 1:
    ##            
    ##    tasks.invokedatapreprocessing.delay(correlationId,pageInfo,userId)
    ##else    :
    ##    utils.insQdb(correlationId,'P','Celery Task Scheduled',pageInfo,userId)

    invokedatapreprocessing(correlationId,pageInfo,userId)
    utils.logger(logger, correlationId, 'INFO', ('Datapreprocessing Completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))


except Exception as e:
        utils.logger(logger,correlationId,'ERROR','Trace',str(requestId))
        utils.save_Py_Logs(logger,correlationId)
        sys.exit()
        #return jsonify({'status': 'false', 'message':str(e.args)}), 200
#else:
        #logger = utils.logger('Get',correlationId)
        #utils.logger(logger,correlationId,'INFO','Task scheduled',str(requestId))
        #utils.save_Py_Logs(logger,correlationId)
        #sys.exit()
        #return jsonify({'status': 'true','message':"Success.."}), 200
