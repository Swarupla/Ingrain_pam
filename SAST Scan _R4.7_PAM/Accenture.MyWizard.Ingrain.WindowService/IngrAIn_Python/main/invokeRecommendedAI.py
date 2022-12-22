import time
start = time.time()
import psutil
cpu=str(psutil.cpu_percent())
memory=str(psutil.virtual_memory()[2])
import platform
import multiprocessing as mp

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



import sys
import trainModels
import base64
import json
from SSAIutils import EncryptData
from pandas import Timestamp
from datetime import datetime

from SSAIutils import utils
#print (sys.argv)
correlationId = sys.argv[1]
requestId = sys.argv[2]
pageInfo = sys.argv[3]
userId = sys.argv[4]
if len(sys.argv) == 6:
    genericflow = False
elif len(sys.argv) > 6 and sys.argv[6] == 'True':
    genericflow = True
else:
    genericflow = False

end = time.time()

try:
    version=sys.argv[5]
    if type(version)==int:
        version=version
    elif version.isnumeric():
        version=int(version)
except:
    version=None

def call_instamodels(Model):
    if Model == 'Logistic Regressor':
        logisticReg()
    elif Model == 'Random Forest Classifier':
        RandomForestClassifier()
    elif Model == 'SVM Classifier':
        SVM()
    elif Model == 'XGBoost Classifier':
        XGB()
    elif Model == 'Lasso Regressor':
        Lasso_Regressor()
    elif Model == 'Ridge Regressor':
        Ridge_Regressor()
    elif Model == 'Random Forest Regressor':
        Random_Forest_Regressor()
    elif Model == 'Generalized Linear Model':
        Generalized_Linear_Model()
    elif Model == 'ARIMA':
        ARIMA()
    elif Model == 'SARIMA':
        SARIMA()
    elif Model == 'ExponentialSmoothing':
        ExponentialSmoothing()
    elif Model == 'Holt-Winters':
        Holt_Winters()

    return

def logisticReg():
    trainModels.invokelogisticregressor(correlationId,'Logistic Regressor',pageInfo,userId,HyperTune=HyperTune,HTId=HTId,version=version)
    return

def RandomForestClassifier():
    trainModels.invokerfcclassification(correlationId,'Random Forest Classifier',pageInfo,userId,HyperTune=HyperTune,HTId=HTId,version=version)
    return

def SVM():
    trainModels.invokesvmclassification(correlationId,'SVM Classifier',pageInfo,userId,HyperTune=HyperTune,HTId=HTId,version=version)
    return

def XGB():
    trainModels.invokegbmclassification(correlationId,'XGBoost Classifier',pageInfo,userId,HyperTune=HyperTune,HTId=HTId,version=version)
    return

def Lasso_Regressor():
    trainModels.invokelassoregressor(correlationId,'Lasso Regressor',pageInfo,userId,HyperTune=HyperTune,HTId=HTId,version=version)                        
    return

def Ridge_Regressor():
    trainModels.invokeridgeregressor(correlationId,'Ridge Regressor',pageInfo,userId,HyperTune=HyperTune,HTId=HTId,version=version)
    return

def Random_Forest_Regressor():
    trainModels.invokerfrregressor(correlationId,'Random Forest Regressor',pageInfo,userId,HyperTune=HyperTune,HTId=HTId,version=version)
    return

def Generalized_Linear_Model():
    trainModels.invokegeneralizedlinearmodel(correlationId,'Generalized Linear Model',pageInfo,userId,HyperTune=HyperTune,HTId=HTId,version=version)
    return

def SVM_Regressor():
    trainModels.invokesvrregressor(correlationId,'SVM Regressor',pageInfo,userId,HyperTune=HyperTune,HTId=HTId,version=version)    
    return
 
def Gradient_Boost_Regressor():
    trainModels.invokegbregressor(correlationId,'Gradient Boost Regressor',pageInfo,userId,HyperTune=HyperTune,HTId=HTId,version=version)
    return

def ARIMA():
    trainModels.invokearimatimeseries(correlationId,'ARIMA',pageInfo,userId,seasonality=False,version=version)
    return

def SARIMA():
    trainModels.invokearimatimeseries(correlationId,'SARIMA',pageInfo,userId,seasonality=True,version=version)
    return

def ExponentialSmoothing():
    trainModels.invokeEStimeseries(correlationId,'ExponentialSmoothing',pageInfo,userId,seasonal=False,version=version)
    return

def Holt_Winters():
    trainModels.invokeHWEStimeseries(correlationId,'Holt-Winters',pageInfo,userId,version=version)
    return

def getproblemtype(correlationId):
    dbconn,dbcollection = utils.open_dbconn('PS_BusinessProblem')
    data = list(dbcollection.find({"CorrelationId" :correlationId}))
    dbconn.close() 
    return data[0]["ProblemType"]  

print (correlationId,requestId,pageInfo,userId)

argParam,modelName,_ = utils.getRequestParams(correlationId,requestId)
argParam = eval(argParam)

HyperTune = argParam.get('IsHyperTuned')
HTId = argParam.get('HTId')
#Model = argParam['Model']

logger = utils.logger('Get',correlationId)
utils.logger(logger, correlationId, 'INFO', ('import took '  + str(end-start)+ ' secs'),str(requestId))
utils.logger(logger,correlationId,'INFO',
                         ('ModelsTraining: '+'CorrelationID :'+str(correlationId)+
                          ' pageInfo :'+pageInfo+
                          ' UserId :'+userId),str(requestId))

if HyperTune != 'True':
                HTId = None
                HyperTune = None

##count = utils.check_CorId("SSAI_UseCase",correlationId,'Model Training')
##if count >= 1:
##           utils.logger(logger,correlationId,'WARNING',('\n'+'ModelsTraining'+'\'n'+"Request for correlation Id :"+str(correlationId)+ "is in Progress"))                              
##           return jsonify({'status': 'false','message':"Request is in Progress"}), 200                  
##    
##utils.insQdb(correlationId,'P','Model Training','Model Training',userId,UniId = HTId)            
            
featureParams = utils.getFeatureSelectionVariable(correlationId)
            #targetVar = utils.getTargetVariable(correlationId)
            #selectedColumn = utils.getSelectedColumn(featureParams["selectedFeatures"],data.columns)
dbconn,dbcollection = utils.open_dbconn("PS_BusinessProblem")
data_json = list(dbcollection.find({"CorrelationId" :correlationId}))
dbconn.close()
target_variable = data_json[0].get('TargetColumn')
selectedColumn = list(set(featureParams['selectedFeatures']+[target_variable]))          

utils.setRetrain(correlationId,False)
#            dbconn,dbcollection = utils.open_dbconn('ME_RecommendedModels')
#            dbcollection.update_one({"CorrelationId":correlationId},{"$set":{"retrain":True}})
#            dbconn.close()
            
#            utils.insInputSample(correlationId,inputdata.head(2))  
                              
dbconn,dbcollection = utils.open_dbconn('ME_RecommendedModels')
models_data = dbcollection.find({"CorrelationId":correlationId})

#Fetch Problem Type
ProblemType = models_data[0].get('ProblemType')

if ProblemType != 'TimeSeries':
    selectedColumn.remove(target_variable)

'''CHANGES START HERE'''
dbconn,dbcollection = utils.open_dbconn('ME_FeatureSelection')
data_json = dbcollection.find({"CorrelationId":correlationId})
#new_features = data_json[0].get("Features_Created")
problemTypeflag=data_json[0].get("NLP_Flag")
clustering_flag = data_json[0].get("Clustering_Flag")
if isinstance(problemTypeflag,type(None)):
    problemTypeflag=False
elif problemTypeflag:
    cluster_columns =data_json[0].get("Cluster_Columns")
if isinstance(clustering_flag,type(None)):
    clustering_flag=True
#Add Features adding in original columns
existingFeatures = []
dbconn,dbcollection = utils.open_dbconn('DE_AddNewFeature')
data_json = dbcollection.find({"CorrelationId":correlationId})
try:
    new_features = data_json[0].get("Features_Created")
except Exception:
    new_features =[]
EnDeRequired = utils.getEncryptionFlag(correlationId)
dbconn,dbcollection = utils.open_dbconn('DE_DataCleanup')
data_json = dbcollection.find({"CorrelationId":correlationId})
#if EnDeRequired :
#    t = base64.b64decode(data_json[0].get('NewAddFeatures'))	
#    data_json[0]['NewAddFeatures']  =  eval(EncryptData.DescryptIt(t))
if len(new_features)>0:
                                                        
    for newCol in data_json[0]['NewAddFeatures']:
        if newCol in new_features:
            for each in data_json[0]["NewAddFeatures"][newCol]:
                try:
                    if each != 'value_check' and each != 'value':
                        existingFeatures.append(data_json[0]["NewAddFeatures"][newCol][each]['ColDrp'])
                except KeyError:
                   utils.logger(logger, correlationId, 'INFO', ('Key error in existingFeatures'+str(each)),str(requestId))

offlineutility = utils.checkofflineutility(correlationId)
if offlineutility:
    inputdata = utils.data_from_chunks_offline_utility(correlationId, collection="DataSet_IngestData",lime=True,recent=None,DataSetUId=offlineutility)
else:
    inputdata = utils.data_from_chunks(correlationId,"PS_IngestedData",lime=True)
inputdata = inputdata.head(2)
#Adding new feature data
if len(new_features)>0:
    selectedColumn = list(set(existingFeatures).union(set(selectedColumn)-set(new_features)))
    #dbproconn,dbprocollection = utils.open_dbconn("DataCleanUP_FilteredData")
    #data_json1 = dbprocollection.find({"CorrelationId" :correlationId})  
    #unique_values_from_db=data_json1[0].get('ColumnUniqueValues')
    #for feature in new_features:
    #    inputdata[feature] = unique_values_from_db[feature][0:2]
#if ProblemType == 'Text_Classification' and 'All_Text' in selectedColumn:
    #selectedColumn.remove('All_Text')
if problemTypeflag:
    if clustering_flag:
        selectedColumn = list(set(selectedColumn)-set(cluster_columns))
        dbproconn,dbprocollection = utils.open_dbconn("ME_FeatureSelection")
        data_json1 = dbprocollection.find({"CorrelationId" :correlationId})  
        final_text_columns=data_json1[0].get('Final_Text_Columns')
        selectedColumn = selectedColumn+final_text_columns
    elif not clustering_flag:
        selectedColumn = list(set(selectedColumn)-set(['All_Text']))
        dbproconn,dbprocollection = utils.open_dbconn("ME_FeatureSelection")
        data_json1 = dbprocollection.find({"CorrelationId" :correlationId})  
        final_text_columns=data_json1[0].get('Final_Text_Columns')
        selectedColumn = selectedColumn+final_text_columns

    selectedColumn = list(set(selectedColumn)-set(new_features))

inputdata = inputdata[selectedColumn]
for column in [col for col in inputdata.columns if inputdata[col].dtype == 'datetime64[ns]']:
    inputdata[column] = inputdata[column].dt.strftime("%d-%m-%Y %H:%M:%S")

'''CHANGES END HERE'''
                
if HyperTune == 'True':
    models = modelName
else:
    #Fetch user selected models    
    #models_d =  models_data[0].get('SelectedModels')        
    #models = [key for key,value in models_d.items() if list(value.values())[0] == 'True']
    models=modelName
    #Insert data sample
    if inputdata.isnull().values.any():
        inputdata.fillna('', inplace=True)
    utils.insInputSample(correlationId,inputdata)  
utils.logger(logger, correlationId, 'INFO', ('Recommended AI started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))+ " for the model "+models+" With CPU: "+cpu+" Memory: "+memory),str(requestId))
 
if not genericflow:
    if ProblemType == 'Classification' or ProblemType=='Multi_Class' or ProblemType == 'Text_Classification':                 
            if models == 'Logistic Regressor':
                trainModels.invokelogisticregressor(correlationId,'Logistic Regressor',pageInfo,userId,HyperTune=HyperTune,HTId=HTId,version=version)
            elif models == 'Random Forest Classifier':                        
                trainModels.invokerfcclassification(correlationId,'Random Forest Classifier',pageInfo,userId,HyperTune=HyperTune,HTId=HTId,version=version)
            elif models == 'SVM Classifier':
                trainModels.invokesvmclassification(correlationId,'SVM Classifier',pageInfo,userId,HyperTune=HyperTune,HTId=HTId,version=version)
            elif models == 'XGBoost Classifier':
                trainModels.invokegbmclassification(correlationId,'XGBoost Classifier',pageInfo,userId,HyperTune=HyperTune,HTId=HTId,version=version)
            
            
    elif ProblemType == 'Regression':                
        #for i in range(len(models)):
            if models == 'Lasso Regressor':
                trainModels.invokelassoregressor(correlationId,'Lasso Regressor',pageInfo,userId,HyperTune=HyperTune,HTId=HTId,version=version)                        
            elif models == 'Ridge Regressor':
                print ("modelsssaaaaaaaaaa")
                trainModels.invokeridgeregressor(correlationId,'Ridge Regressor',pageInfo,userId,HyperTune=HyperTune,HTId=HTId,version=version)
            elif models == 'Random Forest Regressor':
                trainModels.invokerfrregressor(correlationId,'Random Forest Regressor',pageInfo,userId,HyperTune=HyperTune,HTId=HTId,version=version)
            elif models == 'Generalized Linear Model':
                trainModels.invokegeneralizedlinearmodel(correlationId,'Generalized Linear Model',pageInfo,userId,HyperTune=HyperTune,HTId=HTId,version=version)
            elif models == 'SVM Regressor':
                trainModels.invokesvrregressor(correlationId,'SVM Regressor',pageInfo,userId,HyperTune=HyperTune,HTId=HTId,version=version)    
            elif models == 'Gradient Boost Regressor':
                trainModels.invokegbregressor(correlationId,'Gradient Boost Regressor',pageInfo,userId,HyperTune=HyperTune,HTId=HTId,version=version)

    elif ProblemType == 'TimeSeries': 
       # for i in range(len(models)):
            if models == 'ARIMA':
                trainModels.invokearimatimeseries(correlationId,'ARIMA',pageInfo,userId,seasonality=False,version=version)
            elif models == 'SARIMA':
                trainModels.invokearimatimeseries(correlationId,'SARIMA',pageInfo,userId,seasonality=True,version=version)
            elif models == 'ExponentialSmoothing':
                trainModels.invokeEStimeseries(correlationId,'ExponentialSmoothing',pageInfo,userId,seasonal=False,version=version)
            elif models == 'Holt-Winters':
                trainModels.invokeHWEStimeseries(correlationId,'Holt-Winters',pageInfo,userId,version=version)
            elif models == 'Prophet':
                trainModels.invokeProphettimeseries(correlationId,'Prophet',pageInfo,userId,version=version)
    #            utils.insQdb(correlationId,'P','Celery Task Scheduled',pageInfo,userId)            
else:
    instaregression = utils.check_instaregression(correlationId)
    instaml = utils.check_instaml(correlationId)
    if instaregression or instaml:
        dbconn,dbcollection = utils.open_dbconn('MLDL_ModelsMaster')
        models_data = dbcollection.find({"_id" :'29529e26-b07c-4138-838b-450a9b126efd'}) 
        dbconn.close() 
        problemtypef = getproblemtype(correlationId)
        allModels = models_data[0].get('Models',{}).get(problemtypef)
        requiredModels = [k for k,v in allModels.items() if v == 'True']
        result = [call_instamodels(requiredModels[x]) for x in range(len(requiredModels))]

    else:
        if ProblemType == 'Classification' or ProblemType=='Multi_Class' or ProblemType == 'Text_Classification':
	        p1 = mp.Process(target = logisticReg)
	        p2 = mp.Process(target = RandomForestClassifier)
	        p3 = mp.Process(target = SVM)
	        p4 = mp.Process(target = XGB)
	        p1.start()
	        p2.start()
	        p3.start()
	        p4.start()
	        p1.join()
	        p2.join()
	        p3.join()
	        p4.join()

        if ProblemType == 'Regression':
	        p1 = mp.Process(target = Lasso_Regressor)
	        p2 = mp.Process(target = Ridge_Regressor)
	        p3 = mp.Process(target = Random_Forest_Regressor)
	        p4 = mp.Process(target = Generalized_Linear_Model)
	        p5 = mp.Process(target = SVM_Regressor)
	        p6 = mp.Process(target = Gradient_Boost_Regressor)
	        p1.start()
	        p2.start()
	        p3.start()
	        p4.start()
	        p5.start()
	        p6.start()
	        p1.join()
	        p2.join()
	        p3.join()
	        p4.join()
	        p5.join()
	        p6.join()

        if ProblemType == 'TimeSeries':
	        p1 = mp.Process(target = ARIMA)
	        p2 = mp.Process(target = SARIMA)
	        p3 = mp.Process(target = ExponentialSmoothing)
	        p4 = mp.Process(target = Holt_Winters)
	        p1.start()
	        p2.start()
	        p3.start()
	        p4.start()
	        p1.join()
	        p2.join()
	        p3.join()
	        p4.join()
utils.logger(logger, correlationId, 'INFO', ('Recommended AI Completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
