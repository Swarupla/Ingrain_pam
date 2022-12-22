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
from wfAnalysis import WF_UploadTestData
from pandas import Timestamp
from datetime import datetime


correlationId = sys.argv[1]
requestId = sys.argv[2]
pageInfo = sys.argv[3]
userId = sys.argv[4]
logger = utils.logger('Get',correlationId)
end = time.time()
argParam,_,_ = utils.getRequestParams(correlationId,requestId)
argParam = eval(argParam)
uploadId = utils.getUniId(correlationId,requestId)

def WFIngestdata(correlationId,uploadId,pageInfo,userId,filepath):
    try:  
        logger = utils.logger('Get',correlationId)
        utils.updQdb(correlationId,'P','10',pageInfo,userId,UniId=uploadId)        
        filesize=WF_UploadTestData.main(correlationId,uploadId,pageInfo,userId,filepath)
        utils.logger(logger,correlationId,'INFO',('Ingesting_Data : ' +          
                                                  'FileSize :'+str(filesize)),str(requestId))        
    except Exception as e:
        utils.logger(logger,correlationId,'ERROR','Trace',str(requestId))
        utils.updQdb(correlationId,'E',e,pageInfo,userId,UniId=uploadId)    
        utils.save_Py_Logs(logger,correlationId)
        sys.exit()
        #return str(e.args)
    
try:
    filepath = argParam['FilePath']
    #uploadId      = argParam['UniId']
    utils.updQdb(correlationId,'P','Task Scheduled',pageInfo,userId,UniId=uploadId)

    logger = utils.logger('Get',correlationId)
    utils.logger(logger, correlationId, 'INFO', ('import took '  + str(end-start)+' secs'+ " with CPU: "+cpu+" Memory: "+memory),str(requestId))
                
    utils.logger(logger,correlationId,'INFO',
                 ('IngestData : '+
                  'CorrelationID :'+str(correlationId)+
                  ' UploadId :'+str(uploadId)+
                  ' pageInfo :'+pageInfo+
                  ' UserId :'+userId+
                  ' filePath :'+filepath),str(requestId)) 
    utils.logger(logger, correlationId, 'INFO', ('what if started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
    WFIngestdata(correlationId,uploadId,pageInfo,userId,filepath)   
    utils.logger(logger, correlationId, 'INFO', ('what if completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))

except Exception as e:        
    utils.logger(logger,correlationId,'ERROR','Trace',str(requestId))   
    utils.save_Py_Logs(logger,correlationId)
    sys.exit()
    #return jsonify({'status': 'false', 'message':str(e.args)}), 200
else:
    utils.logger(logger,correlationId,'INFO','Task scheduled',str(requestId))
    #utils.save_Py_Logs(logger,correlationId)
    sys.exit()
    #return jsonify({'status': 'true','message':"Success.."}), 200 

