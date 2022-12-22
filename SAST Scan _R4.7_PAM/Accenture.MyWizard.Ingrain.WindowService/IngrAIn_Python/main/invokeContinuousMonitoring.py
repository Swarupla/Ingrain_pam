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
from ContinuousMonitoring import ModelMonitoring
from datetime import datetime
from pandas import Timestamp

correlationId = sys.argv[1]
requestId = sys.argv[2]
pageInfo = sys.argv[3]
userId = sys.argv[4]
end= time.time()

def Model_Monitoring(correlationId,requestId,pageInfo,userId,deployed_accuracy,model_name):  
     try:  
        logger = utils.logger('Get',correlationId)
        utils.updQdb(correlationId,'P','5',pageInfo,userId,UniId=requestId)   
        ModelMonitoring.ModelingMetrics(correlationId,requestId,pageInfo,userId,deployed_accuracy,model_name)        
     except Exception as e:
        utils.logger(logger,correlationId,'ERROR','Trace',str(requestId))
        utils.updQdb(correlationId,'E',str(e),pageInfo,userId,UniId=requestId)    
        utils.save_Py_Logs(logger,correlationId)
        sys.exit()
        #return str(e.args)  

logger = utils.logger('Get',correlationId)

try:
    argParam,model,_ = utils.getRequestParams(correlationId,requestId)
    argParam = eval(argParam)

#    wfId = argParam['WfId']
#    desiredValue = argParam['desired_value']
    deployed_accuracy = float(argParam["DeployedAccuracy"])
    model_name = argParam["ModelName"]
    logger = utils.logger('Get',correlationId)
    utils.logger(logger, correlationId, 'INFO', ('import took '  + str(end-start) + ' secs'+ " with CPU: "+cpu+" Memory: "+memory),str(requestId))
                
    utils.logger(logger,correlationId,'INFO',
                             ('IngestData '+
                              'CorrelationID :'+str(correlationId)+
                              ' WFId :'+str(requestId)+
                              ' Model :'+str(model)+
                              ' pageInfo :'+pageInfo+
                              ' UserId :'+userId
                              ),str(requestId)) 
    utils.updQdb(correlationId,'P','Task Scheduled',pageInfo,userId,UniId = requestId)      
    utils.logger(logger, correlationId, 'INFO', ('Model monitoring started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
    Model_Monitoring(correlationId,requestId,pageInfo,userId,deployed_accuracy,model_name)
    utils.logger(logger, correlationId, 'INFO', ('Model monitoring Completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))

except Exception as e:        
    logger = utils.logger('Get',correlationId)
    utils.logger(logger,correlationId,'ERROR','Trace',str(requestId))   
    utils.save_Py_Logs(logger,correlationId)
    sys.exit()
    #return jsonify({'status': 'false', 'message':str(e.args)}), 200
else:
    logger = utils.logger('Get',correlationId)
    utils.logger(logger,correlationId,'INFO','Task scheduled',str(requestId))
    utils.save_Py_Logs(logger,correlationId)
    sys.exit()
    #return jsonify({'status': 'true','message':"Success.."}), 200
