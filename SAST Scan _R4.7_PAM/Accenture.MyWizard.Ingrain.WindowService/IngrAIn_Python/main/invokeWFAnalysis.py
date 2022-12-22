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
from wfAnalysis import WF_Analysis
from wfAnalysis import WF_Analysis_TimeSeries
from datetime import datetime
from pandas import Timestamp


correlationId = sys.argv[1]
requestId = sys.argv[2]
pageInfo = sys.argv[3]
userId = sys.argv[4]
end = time.time()
def WFAnalysis(correlationId,wfId,model,pageInfo,userId,bulk):  
     try:  
        logger = utils.logger('Get',correlationId)
        utils.updQdb(correlationId,'P','5',pageInfo,userId,UniId=wfId)   
        if model.startswith(("ARIMA","SARIMA","ExponentialSmoothing","Holt-Winters","Prophet")):
            WF_Analysis_TimeSeries.main(correlationId,wfId,model,pageInfo,userId)
        else:
            WF_Analysis.main(correlationId,wfId,model,pageInfo,userId,bulk)        
     except Exception as e:
        utils.logger(logger,correlationId,'ERROR','Trace',str(requestId))
        utils.updQdb(correlationId,'E',e,pageInfo,userId,UniId=wfId)    
        utils.save_Py_Logs(logger,correlationId)
        sys.exit()
        #return str(e.args)  

try:
    argParam,model,_ = utils.getRequestParams(correlationId,requestId)
    argParam = eval(argParam)

    wfId = argParam['WfId']
    bulk = argParam['Bulk']
    logger = utils.logger('Get',correlationId)
    utils.logger(logger, correlationId, 'INFO', ('import took '  + str(end-start)+' secs'+" with CPU: "+cpu+" Memory: "+memory),str(requestId))
                
    utils.logger(logger,correlationId,'INFO',
                             ('IngestData : '+
                              'CorrelationID :'+str(correlationId)+
                              ' WFId :'+str(wfId)+
                              ' Model :'+str(model)+
                              ' pageInfo :'+pageInfo+
                              ' UserId :'+userId+
                              #'\n'+'Steps :'+steps+
                              ' Bulk :'+bulk)) 
    utils.updQdb(correlationId,'P','Task Scheduled',pageInfo,userId,UniId = wfId)   
    utils.logger(logger, correlationId, 'INFO', ('what-if analysis started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
    WFAnalysis(correlationId,wfId,model,pageInfo,userId,bulk)   
    utils.logger(logger, correlationId, 'INFO', ('what-if analysis Completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))

except Exception as e:        
        utils.logger(logger,correlationId,'ERROR','Trace',str(requestId))   
        utils.save_Py_Logs(logger,correlationId)
        sys.exit()
        #return jsonify({'status': 'false', 'message':str(e.args)}), 200
else:
    utils.logger(logger,correlationId,'INFO','Task scheduled',str(requestId))
    utils.save_Py_Logs(logger,correlationId)
    sys.exit()
    #return jsonify({'status': 'true','message':"Success.."}), 200
