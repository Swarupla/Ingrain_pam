import time
start = time.time()
import psutil
cpu=str(psutil.cpu_percent())
memory=str(psutil.virtual_memory()[2])
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
import platform
if platform.system() == 'Linux':
    conf_path = '/IngrAIn_Python/main/pythonconfig.ini'
    work_dir = '/IngrAIn_Python'
elif platform.system() == 'Windows':
    conf_path = '\IngrAIn_Python\main\pythonconfig.ini'
    work_dir = '\IngrAIn_Python'

#from main import file_encryptor
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
from models.classification import XGB
from models.classification import LogisticReg
from models.classification import SVM
from models.classification import RandomForestClassifier
from models.regression import Lasso_Regressor
from models.regression import Ridge_Regressor
from models.regression import gb_Regressor
from models.regression import Random_Forest_Regressor
from models.regression import SVR_Regressor
from models.timeseries import ARIMA
#from models.timeseries import ExponentialSmoothing
from models.timeseries import DoubleExponentialSmoothing
from models.timeseries import HoltWintersES
#from models.timeseries import Prophet
from models.regression import Generalized_Linear_Model 
import sys
from datetime import datetime
from pandas import Timestamp
end = time.time()
def invokegeneralizedlinearmodel(correlationId,modelName,pageInfo,userId,HyperTune=None,HTId=None,version=None):
    pType = utils.fetchProblemType(correlationId)[0]
    try:  
        logger = utils.logger('Get',correlationId)
        if HyperTune=='True':
            modelName = str(modelName)+'_'+str(correlationId)+'_'+str(HTId)
        utils.logger(logger, correlationId, 'INFO', ('import took '  + str(end-start)+ ' secs'+" with CPU: "+cpu+" Memory: "+memory),str(None))
        utils.updQdb(correlationId,'P','10',pageInfo,userId,modelName = modelName,problemType=pType,UniId=HTId)        
        utils.logger(logger, correlationId, 'INFO', ('Generalized_Linear_Model started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        Generalized_Linear_Model.main(correlationId,modelName,pageInfo,userId,HyperTune,HTId,version)        
        utils.logger(logger, correlationId, 'INFO', ('Generalized_Linear_Model completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
    except Exception as e:
        utils.logger(logger,correlationId,'ERROR','Trace',str(None))
        utils.save_Py_Logs(logger,correlationId)
        sys.exit()



def invokegbmclassification(correlationId,modelName,pageInfo,userId,HyperTune=None,HTId=None,version=None): 
    pType = utils.fetchProblemType(correlationId)[0]
    try:  
        logger = utils.logger('Get',correlationId)
        if HyperTune=='True':
            modelName = str(modelName)+'_'+str(correlationId)+'_'+str(HTId)
        utils.logger(logger, correlationId, 'INFO', ('import took '  + str(end-start)+ ' secs'),str(None))  
        utils.updQdb(correlationId,'P','10',pageInfo,userId,modelName = modelName,problemType=pType,UniId=HTId)        
        utils.logger(logger, correlationId, 'INFO', ('XGB started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        XGB.main(correlationId,modelName,pageInfo,userId,HyperTune,HTId,version)        
        utils.logger(logger, correlationId, 'INFO', ('XGB completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
    except Exception as e:
        utils.logger(logger,correlationId,'ERROR','Trace',str(None))
        utils.save_Py_Logs(logger,correlationId)
        sys.exit()
        #sys.exit()


def invokerfcclassification(correlationId,modelName,pageInfo,userId,HyperTune=None,HTId=None,version=None): 
    pType = utils.fetchProblemType(correlationId)[0]
    #print(pType)
    try:  
        logger = utils.logger('Get',correlationId)
        if HyperTune=='True':
            modelName = str(modelName)+'_'+str(correlationId)+'_'+str(HTId)
        utils.updQdb(correlationId,'P','10',pageInfo,userId,modelName = modelName,problemType=pType,UniId=HTId)        
        utils.logger(logger, correlationId, 'INFO', ('import took '  + str(end-start)+ ' secs'),str(None))
        utils.logger(logger, correlationId, 'INFO', ('RandomForestClassifier started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        RandomForestClassifier.main(correlationId,modelName,pageInfo,userId,HyperTune,HTId,version)        
        utils.logger(logger, correlationId, 'INFO', ('RandomForestClassifier completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
    except Exception as e:
        utils.logger(logger,correlationId,'ERROR','Trace',str(None))
        utils.save_Py_Logs(logger,correlationId)
        sys.exit()
        #sys.exit()


def invokesvmclassification(correlationId,modelName,pageInfo,userId,HyperTune=None,HTId=None,version=None):  
    pType = utils.fetchProblemType(correlationId)[0]
    try:  
        logger = utils.logger('Get',correlationId)
        if HyperTune=='True':
            modelName = str(modelName)+'_'+str(correlationId)+'_'+str(HTId)
        utils.updQdb(correlationId,'P','10',pageInfo,userId,modelName = modelName,problemType=pType,UniId=HTId)        
        utils.logger(logger, correlationId, 'INFO', ('import took '  + str(end-start)+ ' secs'),str(None))     
        utils.logger(logger, correlationId, 'INFO', ('SVM started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        SVM.main(correlationId,modelName,pageInfo,userId,HyperTune,HTId,version)        
        utils.logger(logger, correlationId, 'INFO', ('SVM completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
    except Exception as e:
        utils.logger(logger,correlationId,'ERROR','Trace',str(None))
        utils.save_Py_Logs(logger,correlationId)
        sys.exit()
        #sys.exit()
     
    
def invokelogisticregressor(correlationId,modelName,pageInfo,userId,HyperTune=None,HTId=None,version=None):
    pType = utils.fetchProblemType(correlationId)[0]
    try:  
        logger = utils.logger('Get',correlationId)
        if HyperTune=='True':
            modelName = str(modelName)+'_'+str(correlationId)+'_'+str(HTId)
        utils.updQdb(correlationId,'P','10',pageInfo,userId,modelName = modelName,problemType=pType,UniId=HTId)        
        utils.logger(logger, correlationId, 'INFO', ('import took '  + str(end-start)+ ' secs'),str(None))
        utils.logger(logger, correlationId, 'INFO', ('LogisticReg started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        LogisticReg.main(correlationId,modelName,pageInfo,userId,HyperTune,HTId,version)        
        utils.logger(logger, correlationId, 'INFO', ('LogisticReg completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
    except Exception as e:
        utils.logger(logger,correlationId,'ERROR','Trace',str(None))
        utils.save_Py_Logs(logger,correlationId)
        
        sys.exit()

    
def invokelassoregressor(correlationId,modelName,pageInfo,userId,HyperTune=None,HTId=None,version=None):
    pType = utils.fetchProblemType(correlationId)[0]
    try:  
        logger = utils.logger('Get',correlationId)
        if HyperTune=='True':
            modelName = str(modelName)+'_'+str(correlationId)+'_'+str(HTId)
        utils.updQdb(correlationId,'P','10',pageInfo,userId,modelName = modelName,problemType=pType,UniId=HTId)        
        utils.logger(logger, correlationId, 'INFO', ('import took '  + str(end-start)+ ' secs'),str(None))     
        utils.logger(logger, correlationId, 'INFO', ('Lasso_Regressor started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        Lasso_Regressor.main(correlationId,modelName,pageInfo,userId,HyperTune,HTId,version)        
        utils.logger(logger, correlationId, 'INFO', ('Lasso_Regressor completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
    except Exception as e:
        utils.logger(logger,correlationId,'ERROR','Trace',str(None))
        utils.save_Py_Logs(logger,correlationId)
        sys.exit()
    
    
def invokeridgeregressor(correlationId,modelName,pageInfo,userId,HyperTune=None,HTId=None,version=None):
    pType = utils.fetchProblemType(correlationId)[0]
    try:  
        logger = utils.logger('Get',correlationId)
        if HyperTune=='True':
            modelName = str(modelName)+'_'+str(correlationId)+'_'+str(HTId)
        utils.updQdb(correlationId,'P','10',pageInfo,userId,modelName = modelName,problemType=pType,UniId=HTId)        
        utils.logger(logger, correlationId, 'INFO', ('import took '  + str(end-start)+ ' secs'),str(None))     
        utils.logger(logger, correlationId, 'INFO', ('Ridge_Regressor started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        Ridge_Regressor.main(correlationId,modelName,pageInfo,userId,HyperTune,HTId,version)        
        utils.logger(logger, correlationId, 'INFO', ('Ridge_Regressor completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
    except Exception as e:
        utils.logger(logger,correlationId,'ERROR','Trace',str(None))
        utils.save_Py_Logs(logger,correlationId)
        sys.exit()

    
def invokegbregressor(correlationId,modelName,pageInfo,userId,HyperTune=None,HTId=None,version=None):
    pType = utils.fetchProblemType(correlationId)[0]
    try:  
        logger = utils.logger('Get',correlationId)
        if HyperTune=='True':
            modelName = str(modelName)+'_'+str(correlationId)+'_'+str(HTId)
        utils.updQdb(correlationId,'P','10',pageInfo,userId,modelName = modelName,problemType=pType,UniId=HTId)        
        utils.logger(logger, correlationId, 'INFO', ('import took '  + str(end-start)+ ' secs'),str(None))  
        utils.logger(logger, correlationId, 'INFO', ('gb_Regressor started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        gb_Regressor.main(correlationId,modelName,pageInfo,userId,HyperTune,HTId,version)        
        utils.logger(logger, correlationId, 'INFO', ('gb_Regressor completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
    except Exception as e:
        utils.logger(logger,correlationId,'ERROR','Trace',str(None))
        utils.save_Py_Logs(logger,correlationId)
        sys.exit()

    
def invokesvrregressor(correlationId,modelName,pageInfo,userId,HyperTune=None,HTId=None,version=None):
    pType = utils.fetchProblemType(correlationId)[0]
    try:  
        logger = utils.logger('Get',correlationId)
        if HyperTune=='True':
            modelName = str(modelName)+'_'+str(correlationId)+'_'+str(HTId)
        utils.updQdb(correlationId,'P','10',pageInfo,userId,modelName = modelName,problemType=pType,UniId=HTId)        
        utils.logger(logger, correlationId, 'INFO', ('import took '  + str(end-start)+ ' secs'),str(None))   
        utils.logger(logger, correlationId, 'INFO', ('SVR_Regressor started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        SVR_Regressor.main(correlationId,modelName,pageInfo,userId,HyperTune,HTId,version)        
        utils.logger(logger, correlationId, 'INFO', ('SVR_Regressor completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
    except Exception as e:
        utils.logger(logger,correlationId,'ERROR','Trace',str(None))
        utils.save_Py_Logs(logger,correlationId)
        sys.exit()

    
def invokerfrregressor(correlationId,modelName,pageInfo,userId,HyperTune=None,HTId=None,version=None):
    pType = utils.fetchProblemType(correlationId)[0]
    try:  
        logger = utils.logger('Get',correlationId)
        if HyperTune=='True':
            modelName = str(modelName)+'_'+str(correlationId)+'_'+str(HTId)
        utils.updQdb(correlationId,'P','10',pageInfo,userId,modelName = modelName,problemType=pType,UniId=HTId)        
        utils.logger(logger, correlationId, 'INFO', ('Random_Forest_Regressor started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        utils.logger(logger, correlationId, 'INFO', ('import took '  + str(end-start)+ ' secs'),str(None))     
        Random_Forest_Regressor.main(correlationId,modelName,pageInfo,userId,HyperTune,HTId,version)        
        utils.logger(logger, correlationId, 'INFO', ('Random_Forest_Regressor completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
    except Exception as e:
        utils.logger(logger,correlationId,'ERROR','Trace',str(None))
        utils.save_Py_Logs(logger,correlationId)
        sys.exit() 

    
def invokearimatimeseries(correlationId,modelName,pageInfo,userId,seasonality=True,version=None):
    try:  
        logger = utils.logger('Get',correlationId)
        #if HyperTune=='True':
         #   modelName = str(modelName)+'_'+str(correlationId)+'_'+str(HTId)
        utils.updQdb(correlationId,'P','5',pageInfo,userId,modelName = modelName,problemType='TimeSeries')
        utils.logger(logger, correlationId, 'INFO', ('import took '  + str(end-start)+ ' secs'),str(None))
        utils.logger(logger, correlationId, 'INFO', ('ARIMA started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        ARIMA.main(correlationId,modelName,pageInfo,userId,seasonality,version)        
        utils.logger(logger, correlationId, 'INFO', ('ARIMA completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
    except Exception as e:
        utils.logger(logger,correlationId,'ERROR','Trace',str(None))
        utils.save_Py_Logs(logger,correlationId)
        sys.exit() 
     
    
def invokeEStimeseries(correlationId,modelName,pageInfo,userId,seasonal=False,version=None):
    try:  
        logger = utils.logger('Get',correlationId)
        #if HyperTune=='True':
         #   modelName = str(modelName)+'_'+str(correlationId)+'_'+str(HTId)
        utils.updQdb(correlationId,'P','10',pageInfo,userId,modelName = modelName,problemType='TimeSeries')        
        utils.logger(logger, correlationId, 'INFO', ('DoubleExponentialSmoothing started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        DoubleExponentialSmoothing.main(correlationId,modelName,pageInfo,userId,seasonal,version)
        utils.logger(logger, correlationId, 'INFO', ('DoubleExponentialSmoothing completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
    except Exception as e:
        utils.logger(logger,correlationId,'ERROR','Trace',str(None))
        utils.save_Py_Logs(logger,correlationId)
        sys.exit() 

    
def invokeHWEStimeseries(correlationId,modelName,pageInfo,userId,version=None):
    try:  
        logger = utils.logger('Get',correlationId)
        #if HyperTune=='True':
         #   modelName = str(modelName)+'_'+str(correlationId)+'_'+str(HTId)
        utils.updQdb(correlationId,'P','10',pageInfo,userId,modelName = modelName,problemType='TimeSeries')        
        utils.logger(logger, correlationId, 'INFO', ('import took '  + str(end-start)+ ' secs'),str(None))
        utils.logger(logger, correlationId, 'INFO', ('HoltWintersES started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        HoltWintersES.main(correlationId,modelName,pageInfo,userId,version)        
        utils.logger(logger, correlationId, 'INFO', ('HoltWintersES completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
    except Exception as e:
        utils.logger(logger,correlationId,'ERROR','Trace',str(None))
        utils.save_Py_Logs(logger,correlationId)
        sys.exit()

def invokeProphettimeseries(correlationId,modelName,pageInfo,userId,version=None):
    try:  
        logger = utils.logger('Get',correlationId)
        #if HyperTune=='True':
         #   modelName = str(modelName)+'_'+str(correlationId)+'_'+str(HTId)
        utils.updQdb(correlationId,'P','10',pageInfo,userId,modelName = modelName,problemType='TimeSeries')        
        utils.logger(logger, correlationId, 'INFO', ('import took '  + str(end-start)+ ' secs'),str(None))       
        utils.logger(logger, correlationId, 'INFO', ('Prophet started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        #Prophet.main(correlationId,modelName,pageInfo,userId,version)        
        utils.logger(logger, correlationId, 'INFO', ('Prophet completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
    except Exception as e:
        utils.logger(logger,correlationId,'ERROR','Trace',str(None))
        utils.save_Py_Logs(logger,correlationId)
        sys.exit()