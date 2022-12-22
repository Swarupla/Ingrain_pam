import numpy as np
import pandas as pd
import statistics as st
from sklearn.linear_model import LinearRegression,Lasso
from sklearn.preprocessing import StandardScaler
from scipy.stats import norm
from collections import Counter
from pymongo import MongoClient
import configparser, os
import file_encryptor
from datetime import datetime
import logging
from pythonjsonlogger import jsonlogger 


#if platform.system() == 'Linux':
#    conf_path = '/IngrAIn_Python/main/pythonconfig.ini'
#    work_dir = '/IngrAIn_Python'
#elif platform.system() == 'Windows':
#    conf_path = '\IngrAIn_Python\main\pythonconfig.ini'
#    work_dir = '\IngrAIn_Python'

config = configparser.RawConfigParser()
#configpath = str(os.getcwd()) + str(conf_path)
#configpath = "/var/www/monteCarloPython/pythonconfig.ini"
conf_path = "/pythonconfig.ini"
configpath = str(os.getcwd()) + str(conf_path)

try:
    config.read(configpath)
except UnicodeDecodeError:
    print("Decrypting Config File : utils.py")
    config = file_encryptor.get_configparser_obj(configpath)

#mainPath = os.getcwd() + work_dir

#sys.path.insert(0, mainPath)
#logpath = config['filePath']['pyLogsPath']
logpath = config['filePath']['pyLogsPath']
if not os.path.exists(logpath):
    os.mkdir(logpath)

def model_data(df,tar_var,inf_cols):
    """
    Input:
        df - Dataframe on which we have to train the Linear Regression model.
    
    Output:
        model - Trained Linear Regression model on the df.
        
    """
    if(len(inf_cols)<=4):
        X = df[inf_cols].values
        y = df[tar_var].values
        lr    = LinearRegression()
    #    alpha =0.3
    #    lr= Lasso(alpha)
        model = lr.fit(X,y)
        coefs = model.coef_
        intercept = model.intercept_
    elif(len(inf_cols)>=4):
        X = df[inf_cols].values
        y = df[tar_var].values
#        lr    = LinearRegression()
        lr=Lasso(alpha=0.01, fit_intercept=True, max_iter = 10000, normalize=False, random_state = 87, tol = 1e-6)
        model = lr.fit(X,y)
        coefs = model.coef_
        intercept = model.intercept_    
        
    return [intercept,coefs]

def random_sample(df,tar_var,new_df,family='normal'):
    """
    Input:
        df - A dataframe containing the features included for regression and the target variable.
        
    Output : 
        stat_df - Contains mean and variance for each column respectively.
        
    """
    np.random.seed(1)
    sampled_df = pd.DataFrame([])
    df_copy = df.copy()
    df_copy = df_copy.drop(labels=tar_var,axis=1)    
    for i in list(df_copy.columns):
        col_arr  = np.array(df[i])
        col_arr = np.append(col_arr,new_df[i].values[0])
        if (family == "normal" ):
            col_mean = st.mean(col_arr)
            col_sd   = st.stdev(col_arr)
            sampled_df[i] = np.random.normal(col_mean,col_sd,int(config["constants"]["iterations"]))#invgaussian
        else:
            col_arr = np.log(col_arr)
            col_mean = st.mean(col_arr)
            col_sd   = st.stdev(col_arr)
            sampled_df[i] = np.random.lognormal(col_mean,col_sd,int(config["constants"]["iterations"]))
#        sampled_df[i] = list(invgauss.rvs(mu=col_mean,loc=col_mean,scale=col_sd,size=1000))
    return sampled_df

def prepare_target_graph_data(sampled_df,model_params,tar_var,new_df,ProblemType):
#    model = joblib.load(model_path[0])
    sampled_df[tar_var] = predict_data(sampled_df,model_params)
    tar_arr =list(sampled_df[tar_var])
    
    #plt.hist(list(sampled_df[tar_var]),bins=13)
    tar_df_stats = sampled_df.describe()
    target_bins = list(np.linspace(min(sampled_df[tar_var]),max(sampled_df[tar_var]),int(config["constants"]["tarbins"])))    
    tar_bins = approx_num_bins(target_bins,tar_arr)
    rng = int(max(tar_bins))-int(min(tar_bins))
    if ProblemType=="ADSP":
        tar_bins=[int(round(i)) for i in tar_bins]
    elif ProblemType=="Generic" and rng<21:
        tar_bins=[(round(i,2)) for i in tar_bins]
    else:
        tar_bins=[int(round(i)) for i in tar_bins]        
    tar_bins=np.array(tar_bins)
    tar_counter = Counter(tar_bins)
    tar_dict = dict(tar_counter)
    
    target_distribution = {"mean":float(tar_df_stats.loc["mean",tar_var]),"standard_deviation":float(tar_df_stats.loc["std",tar_var])}
    prediction = predict_data(new_df,model_params)
    if(prediction[0]<0):
        prediction[0]=0
    uncertainty = (norm.cdf(prediction,target_distribution["mean"],target_distribution["standard_deviation"]))*100
    if str(uncertainty[0]) == ("nan"):
        uncertainty = 100  
    else:
        uncertainty = round(uncertainty[0])
    target_hist = target_distribution
    if ProblemType=="ADSP":
        target_bins=[int(round(i)) for i in target_bins]
    elif ProblemType=="Generic" and rng<21:
        target_bins=[(round(i,2)) for i in target_bins] 
    else:
        target_bins=[int(round(i)) for i in target_bins]
    hist=[]
    for i in target_bins:
        if i in list(tar_dict.keys()):
            hist.append({"XAxisValues":i,"YAxisValues":tar_dict[i]})
        else:
            hist.append({"XAxisValues":i,"YAxisValues":0})
    target_hist.update({"histogram":hist})
    target_dict = {}
    target_dict.update({"TargetValues":tar_dict})
    return target_hist,target_dict,uncertainty,prediction

def predict_data(sampled_df,model_params):
    prediction = []
    for _,row in enumerate(sampled_df.iterrows()):
         pred_df = (pd.DataFrame([dict(row[1])]))
         pred_vals = (list(pred_df.values[0]))
         pred = np.dot(model_params[1],pred_vals)
         pred = (pred + model_params[0])
         prediction.append(pred)
    return (prediction)

def prepare_influencer_graph_data(sampled_df,tar_var,new_df,inf_cols):
    inf_arr= {}
    inf_df_stats={}
    inf_dist={}
    inf_dict={}
    uncertainty_dict = {}
    hist_dict = {}
    for i in inf_cols:
        inf_arr.update({i:sampled_df[i]})
        inf_df_stats.update({ i : inf_arr[i].describe()})
        
        influencer_bins = list(np.linspace(min(sampled_df[i]),max(sampled_df[i]),int(config["constants"]["infbins"])))    
        inf_bins = approx_num_bins(influencer_bins,inf_arr[i])
        inf_bins=[int(round(j)) for j in inf_bins]
        inf_bins=np.array(inf_bins)
        inf_counter = Counter(inf_bins)
        inf_dict.update({ i : dict(inf_counter)})
        inf_dist.update({i: {"mean":float(inf_df_stats[i].loc["mean"]),"standard_deviation":float(inf_df_stats[i].loc["std"])}})
        uncertainty = (norm.cdf(new_df[i][0],inf_dist[i]["mean"],inf_dist[i]["standard_deviation"]))*100
        if str(uncertainty) == ("nan"):
            uncertainty = 100  
        else:
            uncertainty = round(uncertainty)
        uncertainty_dict.update({i+" certainty":uncertainty})
        influencer_bins=[int(round(i)) for i in influencer_bins]
        hist=[]
        for j in influencer_bins:
            if j in list(inf_dict[i].keys()):
                hist.append({"XAxisValues":j,"YAxisValues":inf_dict[i][j]})
            else:
                hist.append({"XAxisValues":j,"YAxisValues":0})
                inf_dict[i].update({j:0})
        hist_dict.update({i:hist})
    return hist_dict,inf_dict,uncertainty_dict,inf_dist

def approx_num_bins(num_unique_arr,pred_arr):
    import numpy as np
    approx_dict = {}
    approx_val=[]
    count=0
    for i in pred_arr:        
        dist=np.float("inf")
        count= count+1
        if str(i)=="nan":
                approx_val.append(0)
        else:
            for j in num_unique_arr:
                temp = abs(float(j)-float(i))
                approx_dict.update({temp : j})
                if temp <= dist:
                    dist = temp
            approx_val.append(approx_dict[dist])
    return approx_val


def feature_selection(ip_df,inf_cols,tar_var):
    
    lassoinput = ip_df.copy()
    scaler = StandardScaler(copy = False)
    scaler.fit(lassoinput)
    Xs = scaler.transform(lassoinput)
    transformed_df = pd.DataFrame(Xs,columns = list(ip_df.columns))
    Xs = np.array(transformed_df[inf_cols])
    Y =np.array(transformed_df[tar_var])
    #print(Xs*std+scaler.mean_)
    clf = Lasso(alpha=0.01, fit_intercept=True, max_iter = 10000, normalize=False, random_state = 87, tol = 1e-6)
    clf.fit(Xs,Y)
    coefs = clf.coef_
    coefs_dict =dict(zip(inf_cols,coefs))
    coefs_dict = sorted(coefs_dict.items(), key=lambda x: x[1], reverse=True)
    d = {}
    for i in coefs_dict:
        d.update({i[0]:i[1]})
    coefs_inf = d 
    if (len(list(coefs_inf.keys()))>=4):
        top_inf = list(coefs_inf.keys())[:4]
    else:
        top_inf = list(coefs_inf.keys())

    return list(coefs_inf.keys()),top_inf

def open_dbconn(collection):
    connection = MongoClient(config['DBconnection']['connectionString'],
                             ssl_pem_passphrase=config['DBconnection']['certificatePassKey'],
                             ssl_certfile=config['DBconnection']['certificatePath'],
                             ssl_ca_certs=config['DBconnection']['certificateCAPath'])

    db = connection.get_default_database()
    db_IngestedData = db[collection]
    return connection, db_IngestedData

def get_dbconn():
    connection = MongoClient(config['DBconnection']['connectionString'],
                             ssl_pem_passphrase=config['DBconnection']['certificatePassKey'],
                             ssl_certfile=config['DBconnection']['certificatePath'],
                             ssl_ca_certs=config['DBconnection']['certificateCAPath'])

    db = connection.get_default_database()    
    return db,connection
def UpdateCDMData(tempId,version,x1,x2,x3):
    dbconn, dbcollection = open_dbconn('TemplateData')
    dbcollection.update_many({"TemplateID": tempId,"Version":version},{'$set':{"InputColumns" : x1,
                                                                       "InputSelection" : x2,
                                                                       "Features" : x3,
                                                                       
                                                                       "ModifiedOn":  datetime.now().strftime('%Y-%m-%d %H:%M:%S')
                                                                      }})

    dbconn.close()

def UpdateProgress(tempId,version,status,progress,msg):
    dbconn, dbcollection = open_dbconn('TemplateData')
    dbcollection.update_many({"TemplateID": tempId,"Version":version},{'$set':{"Status": status ,
                                                                       "Progress": progress,
                                                                       "Message":msg
                                                                      }})

    dbconn.close()
    
class CustomJsonFormatter(jsonlogger.JsonFormatter):
    def add_fields(self, log_record, record, message_dict):
        super(CustomJsonFormatter, self).add_fields(log_record, record, message_dict)
        if not log_record.get('asctime'):
            # this doesn't use record.created, so it is slightly off
            now = datetime.utcnow().strftime('%Y-%m-%dT%H:%M:%S.%fZ')
            log_record['DateTime'] = now
        else:
            log_record['DateTime']	= log_record.get('asctime')
        if log_record.get('levelname'):
            log_record['LogType'] = log_record['levelname'].upper()
        else:
            log_record['LogType'] = record.levelname
        #if log_record.get('uniqueId'):
         #   log_record['UniqueId'] = log_record.get('uniqueId')
        #else:
         #   log_record['UniqueId'] = record.uniqueId	
        if log_record.get('message'):
            log_record['Message'] = log_record['message']
        else:
            log_record['Message'] = record.message
        if log_record.get('TemplateId'):
            log_record['TemplateUId'] = log_record['TemplateId']
        else:
            log_record['TemplateUId'] = record.TemplateId
        if log_record['LogType'] == "ERROR":
            log_record['Exception'] = log_record["exc_info"]
            del log_record['exc_info']

        del log_record['message']
        #del log_record['uniqueId']
        del log_record['TemplateId']
		
			
def json_translate(obj):
    if isinstance(obj, CustomJsonFormatter):
        return {"TemplateId":obj.TemplateId}
def processId():
    pid = os.getpid()
    return(str(pid))
def logger(logger, tempId, level=None, msg=None):
    if logger == 'Get':
        d = datetime.now()
        logger = logging.getLogger(tempId)
        if len(logger.handlers) > 0:
            for handler in logger.handlers:
                logger.removeHandler(handler)
            #        handler = logging.StreamHandler()

        handler = logging.FileHandler(
            logpath + 'Python_MonteCarlo' + '_' + str(d.day) + '_' + str(d.month) + '_' + str(d.year) + '.log')
        #formatter = logging.Formatter('%(asctime)s - %(levelname)s - %(message)s')
        formatter = CustomJsonFormatter(json_default=json_translate)

        #formatter = jsonlogger.JsonFormatter('%(asctime)s - %(levelname)s - %(message)s - %(process)d - %(uniqueId)s',rename_fields={"asctime": "TimeStamp", "levelname": "Level","message":"MessageTemplate"},json_default=json_translate)
        handler.setFormatter(formatter)
        logger.addHandler(handler)
		
        return logger
    elif logger != 'Get':
        if level == 'INFO':
            logger.setLevel(logging.INFO)
            logger.info(msg,extra={"TemplateId":tempId,"ProcessId":processId()})
        elif level == 'DEBUG':
            logger.setLevel(logging.DEBUG)
            logger.debug(msg)
        elif level == 'WARNING':
            logger.setLevel(logging.WARNING)
            logger.warning(msg)
        elif level == 'ERROR':
            logger.setLevel(logging.ERROR)
            if msg == 'Trace':
                logger.error('Trace', exc_info=True,extra={"TemplateId":tempId,"ProcessId":processId()})
            else:
                logger.error(msg)
        elif level == 'CRITICAL':
            logger.setLevel(logging.CRITICAL)
            if msg == 'Trace':
                logger.critical(exc_info=True)
            else:
                logger.critical(msg)
