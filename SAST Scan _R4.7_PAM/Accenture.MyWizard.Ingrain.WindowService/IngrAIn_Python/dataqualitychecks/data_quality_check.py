# -*- coding: utf-8 -*-
"""
Created on Wed Jan 16 11:53:27 2019

@author: s.siddappa.dinnimani
"""
import platform

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
#import psutil
import os
# importing libraries
import numpy as np
import pandas as pd
import datetime
import re
# from imblearn.over_sampling import SMOTE
#import scipy.stats as ss
#import itertools
#import math
from SSAIutils import utils
import base64
#from datapreprocessing import Add_Features
from datetime import datetime
#from pandas import Timestamp
import json
from SSAIutils import EncryptData
global num_type, text_cols, DATE_FORMATS, ordinal_vals, EnDeRequired 
# numerical types
num_type = ['float64', 'int64', 'int', 'float', 'float32', 'int32']
num_type_int = ['int64', 'int', 'int32']
num_type_float = ['float', 'float32', 'float64']

# text columns
text_cols = ["description", "comment", "notes", "resolution", "address"]

# ordinal
ordinal = ['priority', 'severity', 'level', 'changetype', 'risk']
ordinal_vals = ['normal', 'new', 'low', 'high', 'parent', 'old', 'child']

# data formats
DATE_FORMATS = ['%m/%d/%Y %I:%M:%S %p', '%Y/%m/%d %H:%M:%S', '%d/%m/%Y %H:%M', '%m/%d/%Y', '%Y/%m/%d', '%m/%d/%Y %H:%M',
                '%d-%m-%Y']


##Function to determine is a dictionary in the values for a row in a particular column. Very useful in terms of DB Query Usecase.
def unaccepted_datatype(data_t_og):
    columns_list=[]
    for col in data_t_og.columns:
        m = (data_t_og[col].map(lambda x : type(x).__name__)=='dict').any()
        if m:
            columns_list.append(col)
    
    if len(columns_list)>0:
        return "The Columns " + str(columns_list[0:])+ " have unsupported datatypes. Kindly go back to the previous screen, remove these columns and try again"
    else:
        return False
    
#custom cascade method
def appendCascadeDf(correlationId):
    dbconn, dbcollection = utils.open_dbconn("SSAI_DeployedModels")
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    dbconn.close()
    cascadeID = data_json[0]["CustomCascadeId"]    
    
    #get the correlationId of source model
    dbconn, dbcollection = utils.open_dbconn("SSAI_CascadedModels")
    data_json = list(dbcollection.find({"CascadedId": cascadeID}))
    dbconn.close()
    modelList = data_json[0]["ModelList"]
    for key,value in modelList.items():
        if value["CorrelationId"] == correlationId:
            modelNumber = int(str(key)[-1])-1
            cascadeCorrelationID = modelList["Model"+str(modelNumber)]["CorrelationId"]
            cascadeModelName = modelList["Model"+str(modelNumber)]["ModelName"]
            break
    
    cascadeUniqueId = str(cascadeModelName)+"_ID"
    
    dbconn, dbcollection = utils.open_dbconn("SSAI_PredictedData")
    data_json = list(dbcollection.find({"CorrelationId": cascadeCorrelationID}))
    dbconn.close()
    cascadeResult = data_json[0]["PredictedResult"]
    cascadeProblemType = data_json[0]["ProblemType"]
    cascadeTarget = data_json[0]["TargetName"]
    cascadeResultDf = pd.DataFrame(cascadeResult)
    cascadeResultDf.rename(columns={"Target": str(cascadeModelName)+"_"+str(cascadeTarget),"ID": cascadeUniqueId}, inplace = True)
    if cascadeProblemType == "Classification" or cascadeProblemType == "Multi_Class":
        probaDf = cascadeResultDf["TargetScore"].apply(pd.Series)
        cascadeResultDf[str(cascadeModelName)+"_Proba1"] = probaDf.max(axis = 1)
        cascadeResultDf.drop(["TargetScore"], axis=1, inplace = True)
    
    currentResultDf = utils.data_from_chunks(corid=correlationId, collection="PS_IngestedData")
    
    dbconn, dbcollection = utils.open_dbconn("PS_BusinessProblem")
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    dbconn.close()
    currentUniqueId = data_json[0]["TargetUniqueIdentifier"]

    try:
        df_merge = pd.merge(cascadeResultDf, currentResultDf, left_on = cascadeUniqueId, right_on = currentUniqueId, suffixes=(False, False))
    except ValueError:
        drop_common_cols = list(set(cascadeResultDf.columns).intersection(set(currentResultDf.columns)))
        cascadeResultDf.drop(drop_common_cols, axis=1, inplace = True)
        df_merge = pd.merge(cascadeResultDf, currentResultDf, left_on = cascadeUniqueId, right_on = currentUniqueId, suffixes=(False, False))
    
    df_merge.drop([cascadeUniqueId], axis=1, inplace = True)
    
    return df_merge

#add feature
def combine_data(correlationId,typeDict):
    logger = utils.logger('Get', correlationId)
    utils.logger(logger, correlationId, 'INFO', ('combine data started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
    dbconn, dbcollection = utils.open_dbconn("PS_BusinessProblem")
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    dbconn.close()
    target_variable = data_json[0]['TargetColumn']
    if data_json[0]['ProblemType'] != "TimeSeries" and data_json[0]['ProblemType'] != "Regression":
        UserInputColumns = data_json[0]['InputColumns']
        Prob_type = None
    elif data_json[0]['ProblemType'] == "Regression":   
        UserInputColumns = data_json[0]['InputColumns']
        Prob_type =  data_json[0]['ProblemType']
    else:
        UserInputColumns = [data_json[0]['TimeSeries']['TimeSeriesColumn']]
        Prob_type = data_json[0]['ProblemType']
    if 'IsCustomColumnSelected' in data_json[0]:
        customtargetvalue = data_json[0]['IsCustomColumnSelected']
        customtargetvalue = eval(customtargetvalue)
    else:
        customtargetvalue = False
    UniqueIdentifir = data_json[0]['TargetUniqueIdentifier']
    if not customtargetvalue:
        UserInputColumns.insert(0, target_variable) 
    if UniqueIdentifir != None:
        UserInputColumns.insert(0, UniqueIdentifir)   
    offlineutility = utils.checkofflineutility(correlationId)
    if offlineutility:
        data_t = utils.data_from_chunks_offline_utility(corid=correlationId, collection="DataSet_IngestData",lime=None, recent=None,DataSetUId=offlineutility)
    else:
        data_t = utils.data_from_chunks(corid=correlationId, collection="PS_IngestedData")    
    original_data = data_t[UserInputColumns]
    dbconn, dbcollection = utils.open_dbconn("DE_AddNewFeature")
    data2_json = list(dbcollection.find({"CorrelationId": correlationId}))
    dbconn.close()
    
    Addfeaturecolumns = data2_json[0]["Features_Created"]
    #call function from utils to load data from db
    data_t1 = utils.data_from_chunks(corid=correlationId, collection="DE_NewFeatureData")
    Addfeaturedata = data_t1[Addfeaturecolumns] 
    for i in Addfeaturecolumns:
        if i in UserInputColumns:
            UserInputColumns.remove(i)
    original_data = original_data[UserInputColumns] 
    combineddata = pd.concat([original_data,Addfeaturedata],axis = 1)
    UniqueValuesCol, typeDict = getUniqueValues(combineddata)      
    utils.logger(logger, correlationId, 'INFO', ('combine data Completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
    return combineddata, typeDict
# IQR outlier function
def outliers_iqr(ys):
    quartile_1, quartile_3 = np.percentile(ys, [25, 75])
    iqr = quartile_3 - quartile_1
    lower_bound = quartile_1 - (iqr * 1.5)
    upper_bound = quartile_3 + (iqr * 1.5)
    outliers = ys[(ys > upper_bound) | (ys < lower_bound)]
    return outliers


def Check_Skew(data_copy):
    Skew = {}
    for y in data_copy.columns:
        if ((data_copy[y].dtype in num_type) or (data_copy[y].dtype in num_type)):
            Skew[y] = data_copy[y].skew()
    newdict = {k: v for k, v in Skew.items() if (v <= -2) or (v>=2)}
    skew_list = list(newdict.keys())
    return skew_list


def updateBinValues(df_q):
    for col in df_q.index:
        r = df_q.BinningValues[col]
        temp = {}
        for cat in r.values():
            temp[cat['SubCatName']] = cat['Value']
        df_q.BinningValues[col] = temp
    return df_q


def calculate_QScore(data_copy, percent_missing, Outlier_Data_Percent, Skewed, incorrect_date, ImBalanced_col):
    Quality_Df = pd.DataFrame({'Missing': percent_missing, 'Outlier': Outlier_Data_Percent,
                               'Skewed': Skewed, 'incorrect_date': incorrect_date,
                               'ImBalanced': ImBalanced_col}, index=data_copy.columns)
    q_score = []
    q_info = {}
    for col in data_copy.columns:
        num_score = []
        stmt_qlty = []
        if data_copy[col].dtype.name in num_type:

            if Quality_Df.Missing[col] > 0:
                num_score.append(Quality_Df.Missing[col])
                stmt_qlty.append("- Missing(" + str(Quality_Df.Missing[col]) + ")")
            else:
                num_score.append(0.0)
                stmt_qlty.append("- Missing(0)")
            if Quality_Df.Outlier[col] > 0:
                num_score.append(Quality_Df.Outlier[col])
                stmt_qlty.append("- Outlier(" + str(Quality_Df.Outlier[col]) + ")")
            else:
                num_score.append(0.0)
                stmt_qlty.append("- Outlier(0)")
            if Quality_Df.Skewed[col] == "Yes":
                num_score.append(10)
                stmt_qlty.append("- Skewed(" + str(Quality_Df.Skewed[col]) + ")")
            else:
                num_score.append(0.0)
                stmt_qlty.append("- Skewed(0)")
            if sum(num_score) > 99:
                q_score.append(0.0)
                quality_string = "Missing + Outlier + Skewed > 99"
            else:
                q_score.append(round(100 - sum(num_score), 3))
                stmt_qlty.insert(0, "100")
                t = round(100 - sum(num_score), 3)
                stmt_qlty.append("= Data Quality(" + str(t) + ")")
            quality_string = " ".join(stmt_qlty)
            q_info[col] = quality_string
        elif data_copy[col].dtype.name == 'category':
            if Quality_Df.Missing[col] > 0:
                num_score.append(Quality_Df.Missing[col])
                stmt_qlty.append("- Missing(" + str(Quality_Df.Missing[col]) + ")")
            else:
                num_score.append(0.0)
                stmt_qlty.append("- Missing(0)")
            if Quality_Df.ImBalanced[col] in [1, 2, 3]:
                num_score.append(10)
                stmt_qlty.append("- ImBalanced(10.0)")

            else:
                num_score.append(0.0)
                stmt_qlty.append("- ImBalanced(0.0)")
            if sum(num_score) > 99:
                q_score.append(0.0)
                quality_string = "Missing + ImBalanced > 99"
            else:
                q_score.append(round(100 - sum(num_score), 3))
                stmt_qlty.insert(0, "100")
                t = round(100 - sum(num_score), 3)
                stmt_qlty.append("= Data Quality(" + str(t) + ")")
            quality_string = " ".join(stmt_qlty)
            q_info[col] = quality_string
        elif data_copy[col].dtype.name == 'datetime64[ns]':
            if Quality_Df.Missing[col] > 0:
                num_score.append(Quality_Df.Missing[col])
                stmt_qlty.append("- Missing/Incorrect Date Format("+ str(Quality_Df.Missing[col])+")")
            else:
                num_score.append(0)
                stmt_qlty.append("- Missing(0)")
            #if Quality_Df.incorrect_date[col] == 1:
           #     num_score.append(10)
          #      stmt_qlty.append("- Incorrect Date Format(10.0)")
         #  else:
          #      num_score.append(0)
           #     stmt_qlty.append("- Incorrect Date Format(0)")
            if sum(num_score) > 99:
                q_score.append(0.0)
                quality_string = "Missing/Incorrect Date Format > 99"
            else:
                q_score.append(round(100 - sum(num_score), 3))
                stmt_qlty.insert(0, "100")
                t = round(100 - sum(num_score), 3)
                stmt_qlty.append("= Data Quality(" + str(t) + ")")
            quality_string = " ".join(stmt_qlty)
            q_info[col] = quality_string

        elif data_copy[col].dtype.name == 'object':
            if Quality_Df.Missing[col] > 99:
                q_score.append(0.0)
                quality_string = "Missing > 99 Hence quality score is zero"
                q_info[col] = quality_string
                continue
            if Quality_Df.Missing[col] > 0:
                # q_score.append(100 - round(Quality_Df.Missing[col],3))
                q_score.append(round(100 - Quality_Df.Missing[col], 3))
                t = round(Quality_Df.Missing[col], 3)
                quality_string = "100- Missing(" + str(t) + ")"
            else:
                q_score.append(100)
                quality_string = "All quality checks are good"
            q_info[col] = quality_string
        else:
            q_info[col] = ""
            q_score.append(-1)
    return q_score, q_info


def getUniqueValues(df):
    UniqueValuesCol = {}
    typeDict = {}
    for col in df.columns:
        typeDict[col] = df.dtypes[col].name
        if df[col].dtype.name == 'datetime64[ns]' or df[col].dtype.name ==  'datetime64[ns, UTC]':
            UniqueValuesCol[col] = list(df[col].dropna().unique().astype('str'))
        if df[col].dtype.name in num_type or df[col].dtype.name == 'category':
            if df[col].dtype.name == 'category':
                df[col] = df[col].dropna().astype('str')
                df[col] = df[col].str.strip()

                #both chr(8228) and chr(46) are for special character "."
                #replacing chr(8228) with (.)
                df[col] = df[col].str.replace(chr(8228),'.')

            UniqueValuesCol[col] = [np.asscalar(np.array([x])) for x in list(df[col].dropna().unique())]
        elif df[col].dtype.name == 'object':
            if len(list(df[col].dropna().unique())) > 2:
                UniqueValuesCol[col] = list(df[col].dropna().unique())[:3]
            else:
                UniqueValuesCol[col] = list(df[col].dropna().unique())
    del df
    return UniqueValuesCol, typeDict



def convert(data, to):
    converted = None
    if to == 'array':
        if isinstance(data, np.ndarray):
            converted = data
        elif isinstance(data, pd.Series):
            converted = data.values
        elif isinstance(data, list):
            converted = np.array(data)
        elif isinstance(data, pd.DataFrame):
            converted = data.as_matrix()
    elif to == 'list':
        if isinstance(data, list):
            converted = data
        elif isinstance(data, pd.Series):
            converted = data.values.tolist()
        elif isinstance(data, np.ndarray):
            converted = data.tolist()
    elif to == 'dataframe':
        if isinstance(data, pd.DataFrame):
            converted = data
        elif isinstance(data, np.ndarray):
            converted = pd.DataFrame(data)
    else:
        raise ValueError("Unknown data conversion: {}".format(to))
    if converted is None:
        raise TypeError('cannot handle data conversion of type: {} to {}'.format(type(data), to))
    else:
        return converted


def correlation_ratio(categories, measurements):
    """
    Calculates the Correlation Ratio (sometimes marked by the greek letter Eta) for categorical-continuous association.
    Answers the question - given a continuous value of a measurement, is it possible to know which category is it
    associated with?
    Value is in the range [0,1], where 0 means a category cannot be determined by a continuous measurement, and 1 means
    a category can be determined with absolute certainty.
    Wikipedia: https://en.wikipedia.org/wiki/Correlation_ratio
    :param categories: list / NumPy ndarray / Pandas Series
        A sequence of categorical measurements
    :param measurements: list / NumPy ndarray / Pandas Series
        A sequence of continuous measurements
    :return: float
        in the range of [0,1]
    """
    categories = convert(categories, 'array')
    measurements = convert(measurements, 'array')
    fcat, _ = pd.factorize(categories)
    cat_num = np.max(fcat) + 1
    y_avg_array = np.zeros(cat_num)
    n_array = np.zeros(cat_num)
    for i in range(0, cat_num):
        cat_measures = measurements[np.argwhere(fcat == i).flatten()]
        n_array[i] = len(cat_measures)
        y_avg_array[i] = np.average(cat_measures)
    y_total_avg = np.sum(np.multiply(y_avg_array, n_array)) / np.sum(n_array)
    numerator = np.sum(np.multiply(n_array, np.power(np.subtract(y_avg_array, y_total_avg), 2)))
    denominator = np.sum(np.power(np.subtract(measurements, y_total_avg), 2))
    if numerator == 0:
        eta = 0.0
    else:
        eta = numerator / denominator
    return eta


def identifyDaType(data_copy, target, Prob_type,customtargetvalue=False,Addfeaturecolumns=[]):
    # replace any space from feature names
    # data_copy.columns = data_copy.columns.str.replace(' ','')
    logger = utils.logger('Get','Info')
    # get headers of dataframe
    names = data_copy.columns.values.tolist()
    maxCat = int(config["DataCuration"]["maxCategories"])
    if Addfeaturecolumns != []:
       names = list(set(names).intersection(set(Addfeaturecolumns))) 
    # checking for when whole column is empty and dropping off
    empty_cols = [col for col in data_copy.columns if data_copy[col].dropna().empty]
    data_copy.drop(columns=empty_cols, inplace=True)
    names = list(set(names).difference(set(empty_cols)))

    # ==============================================================================
    # #dataType identification
    # ==============================================================================

    #        date_regex = "\d{1,4}[/-]\d{1,2}[/-]\d{1,4}|\t|:?\d{2}:?\d{2}:?\d{2}"
    date_regex = "[\d]{1,4}[/-]+[\d]{1,2}[/-]+[\d]{1,4}[\t\s]\d{2}:\d{2}:\d{2}|[\d]{1,4}[/-]+[\d]{1,2}[/-]+[\d]{1,4}"
    date_cols = []
    incorrect_date_format = []
    for col in names:

        data_copy[col].dropna(inplace=True)
        arr = list(range(len(data_copy[col])))  # resetting column index
        data_copy[col].index = arr
        if data_copy[col].dtype == 'datetime64[ns]':
            date_cols.append(col)

        if data_copy[col].dtype.name == 'datetime64[ns, UTC]':
            date_cols.append(col)
            
            data_copy[col] = data_copy[col].astype('datetime64[ns]')
        if type(data_copy[col][0]) == datetime:
            date_cols.append(col)	
            data_copy[col] = pd.to_datetime(data_copy[col],utc=True)			
            data_copy[col] = data_copy[col].astype('datetime64[ns]')

        elif data_copy[col].dtype.name == 'object':
            date = list(data_copy[col].unique())
            if bool(re.match(date_regex, str(date[0]))):
                try:
                    data_copy[col] = pd.to_datetime(data_copy[col])
                    date_cols.append(col)
                    incorrect_date_format.append(col)
                except:
                    utils.logger(logger,'info','INFO',('Identify datatype function in Data_quality_check'),str(None))
        else:
            utils.logger(logger,'info','INFO',"Identify datatype function in Data_quality_check",str(None))
    # dates type columns are removed
    names = list(set(names).difference(set(date_cols)))
    id_cols = []
    for col in names:
        if data_copy[col].dtype == 'object':
            word_list = []
            data_copy[col].dropna(inplace=True)
            arr = list(range(len(data_copy[col])))  # resetting column index
            data_copy[col].index = arr
            for word in data_copy[col].values:
                word = str(word)
                word_list.append(len(word.split()))
            Avg_word_count = round(sum(word_list) * 1.0 / len(data_copy[col]))
            
            uniquePercent = len(data_copy[col].unique()) * 1.0 / len(data_copy[col]) * 100
            # id columns
            if Avg_word_count < 3 and uniquePercent > 95:
                id_cols.append(col)
            # checking for email id columns
            if re.match(r"(^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$)", str(data_copy[col][0])) != None:
                id_cols.append(col)
        if col.lower == id:
            id_cols.append(col)

    # feature_imp_cols_drop = id_cols + date_cols
    # id  type columns are removed
    names = list(set(names).difference(set(id_cols)))

    UnknownTypes = []
    text_cols = []
    # chekings for categorical,countinues and rext data
    for col in names:
        if data_copy[col].dtype.name == 'bool':
            data_copy[col] = data_copy[col].astype('category')
        data_copy[col].dropna(inplace=True)
        # calculating unique percenatge for values in column
        uniquePercent = len(data_copy[col].unique()) * 1.0 / len(data_copy[col]) * 100
        # classifying categorical and text column based on unique percentage of values in column 2
        if data_copy[col].dtype == 'object':
            word_list = []
            for word in data_copy[col].values:
                word = str(word)
                word_list.append(len(word.split()))
            Avg_word_count = round(sum(word_list) * 1.0 / len(data_copy[col]))

            if uniquePercent < 10 and Avg_word_count <= 3 and len(data_copy[col].unique()) < maxCat:
                data_copy[col] = data_copy[col].astype('category')
            elif uniquePercent > 10 and Avg_word_count > 3:
                data_copy[col] = data_copy[col].astype(str)
                text_cols.append(col)
            elif col.lower() in text_cols:
                data_copy[col] = data_copy[col].astype('object')
                text_cols.append(col)
            else:
                UnknownTypes.append(col)
                

    incorrect_date = pd.Series(data=1, index=incorrect_date_format)
    incorrect_date = incorrect_date.reindex(incorrect_date.index.union(data_copy.columns))
    incorrect_date.fillna(0, inplace=True)
    if (not customtargetvalue) or (customtargetvalue and bool(Addfeaturecolumns)):
        if Prob_type != "TimeSeries" and Prob_type != "Regression":
            uniquePercent = len(data_copy[target].unique()) * 1.0 / len(data_copy[target]) * 100
            if uniquePercent < 10 and len(data_copy[target].unique()) < maxCat and ((not(target in Addfeaturecolumns and customtargetvalue)) or not customtargetvalue):
                data_copy[target] = data_copy[target].astype('category')
            else:
                try:
                    data_copy[target] = data_copy[target].astype('float64')
                except:
                    data_copy[target] = data_copy[target].astype('category')

        else:
            try:
                data_copy[target] = data_copy[target].astype('float64')
            except:
                data_copy[target] = data_copy[target].astype('category')
    return data_copy, id_cols, date_cols, text_cols, incorrect_date


# ==============================================================================
# correlation
# ==============================================================================
def cramers_corrected_stat(x,col1,col2):
    """ calculate Cramers V statistic for categorical-categorical association.
        uses correction from Bergsma and Wicher,
        Journal of the Korean Statistical Society 42 (2013): 323-328
    """
    confusion_matrix = x.groupby([col1,col2])[col2].count().unstack().fillna(0)
    if len(list(confusion_matrix.shape)) > 1:
        import scipy.stats as ss
        try:
            chi2 = ss.chi2_contingency(confusion_matrix)[0]
        except:
            confusion_matrix = pd.crosstab(x[col1], x[col2])
            chi2 = ss.chi2_contingency(confusion_matrix)[0]
        n = confusion_matrix.sum().sum()
        phi2 = chi2 / np.float(n)
        r, k = confusion_matrix.shape
        phi2corr = max(0, phi2 - ((k - 1) * (r - 1)) / (n - 1))
        rcorr = r - ((r - 1) ** 2) / (n - 1) * 1.0
        kcorr = k - ((k - 1) ** 2) / (n - 1) * 1.0
        return np.sqrt(phi2corr / min((kcorr - 1), (rcorr - 1)))
    else:
        return False

# ==============================================================================
# correlation
# ==============================================================================
def CheckCorrelation(data_copy, cols_to_remove, target,customtargetvalue=False,Addfeaturecolumns=[]):
    import itertools
    corr_cols = list(set(data_copy.columns).difference(set(cols_to_remove)))
    corrM = np.zeros((len(corr_cols), len(corr_cols)))
    for col1, col2 in itertools.combinations(corr_cols, 2):
        x = data_copy[[col1, col2]].dropna()

        if not x.empty:
            if x[col1].dtype.name in num_type and x[col2].dtype.name in num_type:
                continue
                # Cramer's V correlation
            elif x[col1].dtype.name == 'category' and x[col2].dtype.name == 'category':
                idx1, idx2 = corr_cols.index(col1), corr_cols.index(col2)
#                crossTabData = x.groupby([col1,col2])[col2].count().unstack().fillna(0)
                td = cramers_corrected_stat(x,col1, col2)
                if td != False:
                    corrM[idx1, idx2] = td
                else:
                    continue
                    # Correlation ratio
            else:
                if x[col1].dtype.name == 'category' and x[col2].dtype.name in num_type:
                    cell = correlation_ratio(x[col1], x[col2])
                    idx1, idx2 = corr_cols.index(col1), corr_cols.index(col2)
                    corrM[idx1, idx2] = cell
                    corrM[idx2, idx1] = cell
                elif x[col2].dtype.name == 'category' and x[col1].dtype.name in num_type:
                    cell = correlation_ratio(x[col2], x[col1])
                    idx1, idx2 = corr_cols.index(col2), corr_cols.index(col1)
                    corrM[idx1, idx2] = cell
                    corrM[idx2, idx1] = cell
                else:
                    continue
        else:
            continue
    Num_cols = data_copy.select_dtypes(num_type).columns
    upper_cat = pd.DataFrame(corrM, index=corr_cols, columns=corr_cols)

    # pearson correlation for numerical values
    upper_num = data_copy[Num_cols].corr().abs()
    #    upper_num = upper_num.where(np.triu(np.ones(upper_num.shape), k=1).astype(np.bool))
    #    upper_cat = upper_cat.where(np.triu(np.ones(upper_cat.shape), k=1).astype(np.bool))
    #    upper_num.fillna(0,inplace = True)

    # retriving columns based on threshold values of correlation
    cat_corr_cols = {}
    num_corr_cols = {}
    upper_cat.fillna(0, inplace=True)
    import math
    upper_cat.replace(to_replace=math.inf, value=0, inplace=True)
    for column in upper_cat.columns:
        t = upper_cat[upper_cat[column] > 0.60].index.tolist()
        if len(t) > 0:
            cat_corr_cols[column] = t
    for column in upper_num.columns:
        p = upper_num[upper_num[column] > 0.80].index.tolist()
        q = upper_num[upper_num[column] < 0].index.tolist()
        c = p + q
        if len(c) > 0:
            c.remove(column)
            num_corr_cols[column] = c

    correlated = {}
    for col in data_copy.columns:
        if col in num_corr_cols.keys() and col in cat_corr_cols.keys():
            correlated[col] = list(set(num_corr_cols[col]).union(set(cat_corr_cols[col])))
        elif col in num_corr_cols.keys() and col not in cat_corr_cols.keys():
            correlated[col] = num_corr_cols[col]
        elif col not in num_corr_cols.keys() and col in cat_corr_cols.keys():
            correlated[col] = cat_corr_cols[col]
        else:
            correlated[col] = []

    corelated_Series = pd.Series(data=list(correlated.values()), index=correlated.keys())

    if (not customtargetvalue) or (customtargetvalue and bool(Addfeaturecolumns)):
        g_list = [target]
    else:
        g_list = []
    corrDict = {}
    for col, val in zip(list(corelated_Series.index), list(corelated_Series.values)):
        if len(val) != 0:
            if col not in g_list:
                t = [i for i in val if i in g_list]
                if len(t) > 0:
                    k = list(set(val).difference(set(t)))
                    if len(k) > 0:
                        corrDict[col] = k
                        g_list = g_list + k
                else:
                    corrDict[col] = val
                    g_list = g_list + val
    return corrDict, corelated_Series



def ChangeDataType(FilterData, columnsModified, columns_modifiedDict):
    #logger = utils.logger('Get', correlationId)
    #utils.logger(logger, correlationId, 'INFO', ('change datatype started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
    text_cols = []
    id_cols = []
    unchangedColumns = {}
    maxCat = int(config["DataCuration"]["maxCategories"])
    # set user assigned dataTypes
    for col in columnsModified:
        if columns_modifiedDict[col] == 'Text':
            text_cols.append(col)
            FilterData[col] = FilterData[col].astype(object)
        elif columns_modifiedDict[col] == 'Id':
            id_cols.append(col)
            FilterData[col] = FilterData[col].astype(str)
        else:
             if  FilterData[col].dtype.name in num_type and columns_modifiedDict[col] == 'datetime64[ns]':
                 msg =str(col)+": Cannot change integer column as datetime64[ns] datatype"
                 unchangedColumns[col] = msg
             else:
                 try:
                     if columns_modifiedDict[col] == "category" and len(FilterData[col].unique()) > maxCat:
                         msg =str(col)+": Cannot change category datatype as is contains to many categorical values" 
                         unchangedColumns[col] = msg
                     else:
                         FilterData[col] = FilterData[col].astype(columns_modifiedDict[col])
                 except:
                     x = FilterData[col].dtype.name
                     y = columns_modifiedDict[col]
                     if  x not in num_type  and y == 'datetime64[ns]':
                         msg = "Cannot change datatype of attribute '"+str(col)+"' from "+x+" to datetime64[ns] as it contains alpha/numeric characters. Please validate once again."
                     elif x == 'float64' and y == 'int64':
                         msg = "Cannot change datatype of attribute '"+str(col)+"' from float64 to int64 as it contains Null values. Please validate once again."
                     elif  x not in num_type and y in num_type:
                         msg = "Cannot change datatype of attribute '"+str(col)+"' from "+x+" to "+y+" as it contains alpha/numeric characters. Please validate once again."
                     else:
                         msg = "Cannot change datatype of attribute '"+str(col)+"' from "+x+" to "+y+". Please validate once again."
                     unchangedColumns[col] = msg
    #utils.logger(logger, correlationId, 'INFO', ('change datatype completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
    return unchangedColumns, FilterData, text_cols, id_cols



def CalculateStastics(data_copy):
    # missing,valid and unique data details
    count_outliers = []
    percent_outliers = []
    outlier_cols = []
    # count_missing = data_copy.isnull().sum()
    count_missing = data_copy.isnull().sum()
    # outliers calculation for numerical columns
    Outlier_Dict = {}
    percent_missing = round(data_copy.isnull().sum() * 100.0 / len(data_copy), 2)
    #if (data_copy==-99999.007).all().any():
    #    percent_missing[data_copy.columns[(data_copy==-99999.007).all()]]=100.0	
    count_unique = [len(data_copy[col].unique()) for col in data_copy.columns]
    percent_unique = [round(len(data_copy[col].unique()) * 100.0 / len(data_copy[col]), 2) for col in data_copy.columns]
    for col in data_copy.columns:
        if data_copy[col].dtype in num_type:
            outliers = outliers_iqr(data_copy[col].dropna())
            if len(outliers) > 0:
                #                Outlier_Dict[col] = outliers
                Outlier_Dict[col] = list(outliers)
                outlier_cols.append(col)
                count_outliers.append(len(outliers))
                percent_outliers.append(len(outliers) * 100.0 / len(data_copy[col]))

    Outlier_Data_Percent = round(pd.Series(data=percent_outliers, index=outlier_cols), 2)
    Outlier_Data_Percent = Outlier_Data_Percent.reindex(Outlier_Data_Percent.index.union(percent_missing.index))
    Outlier_Data_Percent.fillna(0, inplace=True)

    Outlier_Data_Count = pd.Series(data=count_outliers, index=outlier_cols)
    Outlier_Data_Count = Outlier_Data_Count.reindex(Outlier_Data_Count.index.union(percent_missing.index))
    Outlier_Data_Count.fillna(0, inplace=True)

    return Outlier_Data_Count, Outlier_Data_Percent, percent_unique, \
           count_unique, percent_missing, count_missing, Outlier_Dict


# ==============================================================================
# class imbalance check for target column
# ==============================================================================
# set target column and perform imbalance check

def CheckImbalance(data_copy, target, Skewed_cols,customtargetvalue=False,Addfeaturecolumns=[]):
    ImBalanced_col = []
    types_of_data = []
    ImbalanceDict = {}
    maxRatio = int(config["DataCuration"]["SmoteRatio"])
    Skewed = pd.Series(data="Not Applicable", index=data_copy.columns)
    for col in data_copy.columns:
        ImbalanceDict[col] = {}
        if data_copy[col].dtype.name == 'category':
            # col_vals = [str(x).lower() for x in np.unique(data_copy[col].dropna())]
            col_vals = [str(x).lower() for x in data_copy[col].unique()]
            s1 = set(col_vals)
            s2 = set(ordinal_vals)

            if col.lower() in ordinal:  # if len(class_Ratio) > 2:

                types_of_data.append("Ordinal")
            elif len(s1.intersection(s2)) > 0:
                types_of_data.append("Ordinal")
            else:
                types_of_data.append("Nominal")

            class_Ratio = round(data_copy[col].value_counts() * 100.0 / len(data_copy), 3).sort_values(ascending=False)
            if len(class_Ratio == 2) and col == target and ((not customtargetvalue ) or (customtargetvalue and bool(Addfeaturecolumns))):
                if max(class_Ratio) >= maxRatio:
                    ImBalanced = 3
                else:
                    ImBalanced = 0
            if len(class_Ratio == 2) and col != target:
                if max(class_Ratio) > 95:
                    ImBalanced = 2
                else:
                    ImBalanced = 0
            if len(class_Ratio) > 2:
                if max(class_Ratio) > 50 or any(class_Ratio[1:] < 5):
                    ImBalanced = 1
                    ImbalanceDict[col] = class_Ratio.to_dict()
                else:
                    ImBalanced = 0
                    # ImbalanceDict[col] = {}
            ImBalanced_col.append(ImBalanced)
        else:
            ImBalanced = -1
            # ImbalanceDict[col] = {}
            types_of_data.append("N/A")
            ImBalanced_col.append(ImBalanced)

        if data_copy[col].dtype.name in num_type:
            if col in Skewed_cols:
                Skewed[col] = "Yes"
            else:
                Skewed[col] = "No"
    return ImBalanced_col, types_of_data, ImbalanceDict, Skewed




def Data_Quality_Checks(data, target, Prob_type=None, DfModified=None, flag=None, text_cols=None, id_cols=None,
                        OrgnalTypes=None, scale_v=None,customtargetvalue=False,Addfeaturecolumns=[],columns_modifiedDict={},correlationId=None):
    t = os.getpid()
    logger = utils.logger('Get', correlationId)
    utils.logger(logger, correlationId, 'INFO', ('dataqualitycheck started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
    if flag == None:
        empty_cols = [col for col in data.columns if data[col].dropna().empty]
        data.drop(columns=empty_cols, inplace=True)
        OrgnalTypes = pd.Series([data[col].dtype.name for col in data.columns], index=data.columns).to_dict()
        if columns_modifiedDict == {}:                              
            data, id_cols, date_cols, text_cols, incorrect_date = identifyDaType(data, target, Prob_type,customtargetvalue=customtargetvalue,Addfeaturecolumns=Addfeaturecolumns)
            if Addfeaturecolumns != []:
                typeDict, target, removedcols, UserInputColumns, dateFmts = utils.get_DataCleanUP_FilteredData(correlationId)
                try:
                    data = data.astype(typeDict)
                except Exception as e:
                    
                    utils.logger(logger, correlationId, 'INFO', ('Add Features Update data in Data_quality_check'),str(None))
                dbconn, dbcollection = utils.open_dbconn("DE_DataCleanup")
                data_json = list(dbcollection.find({"CorrelationId": correlationId})) 
                dbconn.close()
                EnDeRequired = utils.getEncryptionFlag(correlationId)
                featurename = data_json[0]['Feature Name']
                if EnDeRequired:
                    import base64
                    x = base64.b64decode(featurename) #En.............................
                    featurename = json.loads(EncryptData.DescryptIt(x))
                for key,value in featurename.items():
                    datatype = featurename[key]['Datatype']
                    if datatype['Id']=='True':
                        id_cols.append(key)
                    elif datatype['Text']=='True':
                        text_cols.append(key)
                    elif datatype['datetime64[ns]']=='True':
                        date_cols.append(key)
        else:
            data, id_cols, date_cols, text_cols, incorrect_date = identifyDaType(data, target, Prob_type)
            for col in columns_modifiedDict:
                if columns_modifiedDict[col] == 'Text':
                    data[col] = data.astype(str)
                    text_cols.append(col)
                else:
                    data[col] = data[col].astype(columns_modifiedDict[col])  
            del columns_modifiedDict
        if (Prob_type == 'TimeSeries' or Prob_type == 'Regression') and ((not customtargetvalue) or (customtargetvalue and bool(Addfeaturecolumns))):
            OrgnalTypes[target] = data[target].dtype.name
        cols_to_remove = id_cols + date_cols + text_cols
        
        try:
            corrDict, corelated_Series = CheckCorrelation(data, cols_to_remove, target,customtargetvalue=customtargetvalue,Addfeaturecolumns=Addfeaturecolumns)
        except Exception:
            minval=99999999999999999999
            for col in data.columns:
                #print("shape::",data_copy[col].shape[0])
                minval= min(minval,data[col].shape[0])
            data = data[0:minval]
            corrDict, corelated_Series = CheckCorrelation(data, cols_to_remove, target,customtargetvalue=customtargetvalue,Addfeaturecolumns=Addfeaturecolumns)
        
       

    elif flag == 2:
        id_cols = []
        text_cols = []
        date_cols = list(data.select_dtypes('datetime64[ns]').columns)
        OrgnalTypes = pd.Series([data[col].dtype.name for col in data.columns], index=data.columns).to_dict()
        incorrect_date = pd.Series(data=0, index=data.columns)
        corrDict, corelated_Series = CheckCorrelation(data, date_cols, target,customtargetvalue=customtargetvalue,Addfeaturecolumns=Addfeaturecolumns)
#        data_copy = data.copy()


    else:
        date_cols = list(data.select_dtypes('datetime64[ns]').columns)
        incorrect_date = pd.Series(data=0, index=DfModified.columns)
        cols_to_remove = id_cols + date_cols + text_cols
        corrDict, corelated_Series = CheckCorrelation(data, cols_to_remove, target,customtargetvalue=customtargetvalue,Addfeaturecolumns=Addfeaturecolumns)
        temp = {}
        for col in DfModified.columns:
            temp[col] = OrgnalTypes[col]
        OrgnalTypes = temp
        data = DfModified.copy()
        del DfModified
    Skewed_cols = Check_Skew(data)
    Outlier_Data_Count, Outlier_Data_Percent, percent_unique, \
    count_unique, percent_missing, count_missing, Outlier_Dict = CalculateStastics(data)
    ImBalanced_col, types_of_data, ImbalanceDict, Skewed = CheckImbalance(data, target, Skewed_cols,customtargetvalue=customtargetvalue,Addfeaturecolumns=Addfeaturecolumns)
    Q_Scores, Q_Info = calculate_QScore(data, percent_missing, Outlier_Data_Percent, Skewed, incorrect_date,
                                        ImBalanced_col)

    columns = data.columns
    # Recommendations = pd.Series(data = None,index = columns)
    ProblemType = pd.Series(data=0, index=columns)
    # Prescriptions = pd.Series(data = None,index = columns)
    # UnknownType = pd.Series(data = 0,index = columns)
    Data_quality_df = pd.DataFrame({'column_name': columns,
                                    'DataType': data.dtypes,
                                    # 'CorelatedWith' : corelated_Series,
                                    'count_missing': count_missing,
                                    'percent_missing': percent_missing,
                                    'count_unique': count_unique,
                                    'percent_unique': percent_unique,
                                    'Outlier_Data': Outlier_Data_Percent,
                                    'Outlier_Data_Count': Outlier_Data_Count,
                                    'ImBalanced_col': ImBalanced_col,
                                    'Binning_Values': list(ImbalanceDict.values()),
                                    'OrdinalNominal': types_of_data,
                                    'IsSkewed': Skewed,
                                    'Data_Quality_Score': Q_Scores,
                                    'Q_Info': list(Q_Info.values()),
                                    'ProblemType': ProblemType,
                                    'originalDtyps': list(OrgnalTypes.values())
                                    }, index=columns)
    
    del count_missing,Outlier_Data_Percent,count_unique,percent_missing,percent_unique,types_of_data,Skewed,Q_Scores
    DtypeDict = {}
    ScaleDict = {}
    Balanced = []
    DateFormat = {}
    # Forming recommendation for each data quality check of columns
    for col in data.columns:
        dtypelist2 = ["category", "float64","int64","datetime64[ns]","Id","Text"]

        O_N = ["Ordinal", "Nominal"]

        if data[col].dtype.name == 'datetime64[ns]':
            DateFormat[col] = 'YYYY-MM-DD HH:MM:SS'
        if Data_quality_df.ImBalanced_col[col] == 0:
            Balanced.append("Yes")
        elif Data_quality_df.ImBalanced_col[col] == -1:
            Balanced.append("Not Applicable")
        else:
            Balanced.append("No")

        if col == target and data[col].dtype.name == 'category' and ((not customtargetvalue) or (customtargetvalue and bool(Addfeaturecolumns))):
            if len(data[col].dropna().unique()) > 2:
                Data_quality_df.ProblemType[col] = 3
            else:
                Data_quality_df.ProblemType[col] = 2
        elif col == target and data[col].dtype.name in num_type and ((not customtargetvalue ) or (customtargetvalue and bool(Addfeaturecolumns))):
            Data_quality_df.ProblemType[col] = 1
        else:
            Data_quality_df.ProblemType[col] = 0
        
        if col in id_cols:
            Data_quality_df.Data_Quality_Score[col] = 0
            Data_quality_df.percent_unique[col] = 100
            Data_quality_df.Q_Info[col] = "Id columns does not add any value hence data quality is zero"
            Dtype = "Id"
        elif col in text_cols:
            Data_quality_df.DataType[col]
            Dtype = "Text"
        elif str(Data_quality_df.DataType[col]) == 'datetime64[ns, UTC]':
            Dtype = "datetime64[ns]"

        else:
            Dtype = str(Data_quality_df.DataType[col])
        
        if Dtype == 'object':
            DtypeDict[col] = []
        else:
      
            dtypelist2.remove(Dtype)
            dtypelist2.insert(0, Dtype)
            DtypeDict[col] = dtypelist2
            for i in range(len(Data_quality_df)):
                if Data_quality_df.index[i] == col:
                    Data_quality_df.loc[col,'DataType'] = Dtype
                    break
                #Data_quality_df.loc[Data_quality_df.index[7] == col , 'DataType'] = Dtype
                #print(col)
                
        #        data_copy[col].dtype.name  == 'category'
        if scale_v != None and data[col].dtype.name == 'category':
            if col in scale_v.keys():
                scale = scale_v[col]
            else:
                scale = str(Data_quality_df.OrdinalNominal[col])
        else:
            scale = str(Data_quality_df.OrdinalNominal[col])
        if scale == 'N/A':
            ScaleDict[col] = []
        else:
            O_N.remove(scale)
            O_N.insert(0, scale)
            ScaleDict[col] = O_N
    Data_quality_df['Balanced'] = Balanced
    utils.logger(logger, correlationId, 'INFO', ('dataqualitycheck completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
    return Data_quality_df, Outlier_Dict, Skewed_cols, text_cols, target, \
           DtypeDict, ScaleDict, data, corrDict, id_cols, corelated_Series, DateFormat


def checkDateNull(df):
    for col in df.columns:
        if df[col].dtype.name == "datetime64[ns]" and df[col].isnull().sum() > 0:
            df[col] = df[col].astype(str)
    return df


def UpdateData(correlationId, pageInfo, userId, flag):
    try:
        logger = utils.logger('Get', correlationId)
      
        EnDeRequired = utils.getEncryptionFlag(correlationId) #En.......................
        if pageInfo != 'DataCleanUpAddFeature':
           utils.updQdb(correlationId, 'P', '20', pageInfo, userId)
       
        utils.logger(logger, correlationId, 'INFO', (
                    'Data_quality_check Updation initiated for flag= ' + str(
                flag) + " correlation Id :" + str(correlationId) + "Data Quality Check"),str(None))
        utils.logger(logger, correlationId, 'INFO', ('updatedata started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))

        typeDict, target, removedcols, UserInputColumns, dateFmts = utils.get_DataCleanUP_FilteredData(correlationId)
        offlineutility = utils.checkofflineutility(correlationId)
        if offlineutility:
            data_t = utils.data_from_chunks_offline_utility(corid=correlationId, collection="DataSet_IngestData",lime=None, recent=None,DataSetUId=offlineutility)
            dbconn, dbcollection = utils.open_dbconn("DE_AddNewFeature")
            data2_json = list(dbcollection.find({"CorrelationId": correlationId}))
            dbconn.close()
            if data2_json != []:
                Addfeaturecolumns = data2_json[0]["Features_Created"]
                #print("UserInputColumns",UserInputColumns)
                if Addfeaturecolumns != []:
                    UserInputColumns = 	list(set(UserInputColumns) - set(Addfeaturecolumns))
                message=unaccepted_datatype(data_t[UserInputColumns])
                if type(message)==str:
                    utils.updQdb(correlationId,'E',message,pageInfo,userId)
                    return
            else:
                utils.logger(logger, correlationId, 'INFO',("Feature name Data_quality_check"),str(None))
        else:
            data_t = utils.data_from_chunks(corid=correlationId, collection="PS_IngestedData")
            dbconn, dbcollection = utils.open_dbconn("DE_AddNewFeature")
            data2_json = list(dbcollection.find({"CorrelationId": correlationId}))
            dbconn.close()
            if len(data2_json)==0:
                Addfeaturecolumns=[]
            else:
                Addfeaturecolumns = data2_json[0]["Features_Created"]
            #print("UserInputColumns",UserInputColumns)
            if Addfeaturecolumns != []:
                UserInputColumns = 	list(set(UserInputColumns) - set(Addfeaturecolumns))
            message=unaccepted_datatype(data_t[UserInputColumns])
            if type(message)==str:
                utils.updQdb(correlationId,'E',message,pageInfo,userId)
                return
        dbconn, dbcollection = utils.open_dbconn("PS_BusinessProblem")
        data_json = list(dbcollection.find({"CorrelationId": correlationId}))
        dbconn.close()
        UserInputColumns2 = data_json[0]['InputColumns']
       
        UniqueIdentifir = data_json[0]['TargetUniqueIdentifier']
        ###############changes for visualization ##########################################
        dbconn1, dbcollection1 = utils.open_dbconn("PS_IngestedData")
        data_json1 = list(dbcollection1.find({"CorrelationId": correlationId}))
        dbconn1.close()            
        previousLastDate = data_json1[0].get("previousLastDate")
        if previousLastDate:
            previousLastDate = pd.to_datetime(previousLastDate).tz_localize(None)
            
#            ps_data = data_t.copy()
            tmp = list(data_t["DateColumn"])
            data_t["DateColumn"] = [i.tz_localize(None) for i in tmp]
            del tmp
            filtered_ps_data = data_t[(data_t["DateColumn"])>(previousLastDate)]
            uid_list = filtered_ps_data[UniqueIdentifir]
            uid_list = list(uid_list.unique())
            ignore_fake_indices = ["\"\"","None",None,"\" \"",np.float("nan"),"nan"]
            uid_list = [i for i in uid_list if i not in ignore_fake_indices]
            
            dbcollection.update_one({"CorrelationId": correlationId},{"$set":{"delta_uids":uid_list}})
            #dbconn.close()
        #############################################################

        
        try:
            FilterData = data_t[UserInputColumns]
            UserInputColumns_combined = UserInputColumns
        except:
            FilterData = data_t[UserInputColumns2]
            UserInputColumns_combined = UserInputColumns2
    
        dbconn, dbcollection = utils.open_dbconn("DE_AddNewFeature")
        data2_json = list(dbcollection.find({"CorrelationId": correlationId}))
        dbconn.close()
        if data2_json!= []:
            Addfeaturecolumns = data2_json[0]["Features_Created"]
            if Addfeaturecolumns !=[]:
                FilterData,typeDict  = combine_data(correlationId,typeDict)
                UserInputColumns_combined = FilterData.keys()[0][0]
                if len(UserInputColumns_combined) == 1:
                    UserInputColumns_combined = list(FilterData.keys())
                f1 = False
            else:
                Addfeaturecolumns = []
                
                f1 = True
                
        else:
            Addfeaturecolumns = []
            f1 = True
        dbconn, dbcollection = utils.open_dbconn("PS_BusinessProblem")
        data3_json = list(dbcollection.find({"CorrelationId": correlationId}))
        dbconn.close()
        if 'IsCustomColumnSelected' in data3_json[0]:
            customtargetvalue = data3_json[0]['IsCustomColumnSelected']
            customtargetvalue = eval(customtargetvalue)
        else:
            customtargetvalue = False
        target_from_ps = data3_json[0]['TargetColumn']
        auto_retrain = utils.getAutoRetrainflag(correlationId,pageInfo)
        if not(target_from_ps not in Addfeaturecolumns and customtargetvalue and Addfeaturecolumns != [] ):
            if not auto_retrain:  
                FilterData.drop(columns=removedcols, inplace=True)
        
                [typeDict.pop(i, None) for i in removedcols]
      
           
            try:
                FilterData = FilterData.astype(typeDict)
            except ValueError:
                for k,v in typeDict.items():
                    if v=="datetime64[ns]":
                        if previousLastDate:
                            FilterData[k].fillna(previousLastDate,inplace=True)
                        typeDict[k]= "datetime64[ns]"
                        FilterData[k].fillna("",inplace=True)
                        FilterData[k] = [pd.to_datetime(i).tz_localize(None) for i in list(FilterData[k])]
                				
                FilterData = FilterData.astype(typeDict)
            df_cleanup, columns_modifiedDict, dtype, scale, scale_modified, OrginalTypes, targetType = utils.get_DataCleanUp(
                correlationId)
        
            if len(list(df_cleanup.index)) > len(list(FilterData.columns)):
                df_cleanup_index_names = list(set(df_cleanup.index) - (set(df_cleanup.index)-set(FilterData.columns)))
                df_cleanup = df_cleanup.loc[df_cleanup_index_names,:]
           
            
            for i in range(len(Addfeaturecolumns )):
                if Addfeaturecolumns[i] not in df_cleanup.index:
                    df_cleanup.loc[Addfeaturecolumns[i]] = [np.nan, np.nan, np.nan, {}, np.nan, np.nan, np.nan, np.nan, np.nan, np.nan, {}, np.nan, np.nan, np.nan]
            df_cleanup = updateBinValues(df_cleanup)
           
            scale_v = {}
            for col in df_cleanup.index:
                if df_cleanup.Scale[col] != {}:
                    for k, v in df_cleanup.Scale[col].items():
                        if v == 'True':
                            scale_v[col] = k
                
                    
                
            
            if not auto_retrain:
                [OrginalTypes.pop(i, None) for i in removedcols]
            utils.logger(logger, correlationId, 'INFO',
                         ('Data_quality_check Fetched Data from Filtered data'),str(None))
            
            columnsModified = list(columns_modifiedDict.keys())
            unchangedColumns, FilterData, text_cols, id_cols = ChangeDataType(FilterData, columnsModified,
                                                                              columns_modifiedDict)
           
            if len(unchangedColumns.keys()) == len(columnsModified) and len(columnsModified) !=0:
                utils.Update_DE_DataCleanup(correlationId, unchangedColumns, 4)
                if pageInfo != 'DataCleanUpAddFeature':
                   utils.updQdb(correlationId, 'C', '100', pageInfo, userId)
                utils.logger(logger, correlationId, 'INFO',
                             ('Data_quality_check no changes are done for' + str(columnsModified) + ""),str(None))
            else:    
                DfModified = FilterData[columnsModified]
              
                if pageInfo != 'DataCleanUpAddFeature':
                   utils.updQdb(correlationId, 'P', '50', pageInfo, userId)
               
                if auto_retrain:
                    Quality_df, Outlier_Dict, Skewed_cols, text_cols, target, DtypeDict, ScaleDict, DfModified, \
                    corrDict, id_cols, corelated_Series, DateFormat = Data_Quality_Checks(FilterData.copy(), target, None,
                                                                                      DfModified, None, text_cols, id_cols,
                                                                                      OrginalTypes, scale_v,customtargetvalue=customtargetvalue,
                                                                                      Addfeaturecolumns=Addfeaturecolumns,columns_modifiedDict=columns_modifiedDict)
                    del columns_modifiedDict,DfModified
                else:
                    FilterData_obj = FilterData.select_dtypes(['object'])
                    FilterData[FilterData_obj.columns] = utils.lambdaDataQaulity(FilterData_obj)
                    del FilterData_obj
                    Quality_df, Outlier_Dict, Skewed_cols, text_cols, target, DtypeDict, ScaleDict, DfModified, \
                    corrDict, id_cols, corelated_Series, DateFormat = Data_Quality_Checks(FilterData.copy(), target, None,
                                                                                      DfModified, flag, text_cols, id_cols,
                                                                                      OrginalTypes, scale_v,customtargetvalue=customtargetvalue,
                                                                                      Addfeaturecolumns=Addfeaturecolumns,columns_modifiedDict=columns_modifiedDict,correlationId=correlationId)
                    del columns_modifiedDict,DfModified
                  
                    
               
               
                if Quality_df.empty:
                    scaleD = {}
                    for col in df_cleanup.index:
                        if df_cleanup.Scale[col] != {}:
                            for k, v in df_cleanup.Scale[col].items():
                                if v == 'True':
                                    O_N = ["Ordinal", "Nominal"]
                                    O_N.remove(k)
                                    O_N.insert(0, k)
                                    scaleD[col] = O_N
                        else:
                            scaleD[col] = []
                    ScaleDict = scaleD
                    scale = scaleD
                   
                if auto_retrain:
                    try:
                        cols_required = utils.gettraincols(correlationId)
                    except:
                        dbconn, dbcollection = utils.open_dbconn("DE_DataCleanup")
                        data_json = list(dbcollection.find({"CorrelationId": correlationId})) 
                        dbconn.close()
                        EnDeRequired = utils.getEncryptionFlag(correlationId)
                        featurename = data_json[0]['Feature Name']
                        if EnDeRequired:
                            import base64
                            x = base64.b64decode(featurename) #En.............................
                            featurename = json.loads(EncryptData.DescryptIt(x))
                        cols_required = []                            
                        for key,value in featurename.items():
                            cols_required.append(key)
                   
                    Quality_df = Quality_df[Quality_df.column_name.isin(cols_required)]
                    
                    removedcols=[]
                    
                    if removedcols == []:
                        removedcols = {}
                                   
                    columns_with_improved_quality = list(set(Quality_df.column_name) - set(df_cleanup.index))
                    for i in range(len(columns_with_improved_quality)):
                        if columns_with_improved_quality[i] not in df_cleanup.index:
                            df_cleanup.loc[columns_with_improved_quality[i]] = [np.nan, np.nan, np.nan, {}, np.nan, np.nan, np.nan, np.nan, np.nan, np.nan, {}, np.nan, np.nan, np.nan]								
                    for i in range(len(removedcols)):
                        if removedcols[i] in OrginalTypes.keys():
                            OrginalTypes.pop(removedcols[i])
 
                

                # unique values
                ColUniqueValues, typeDict = getUniqueValues(FilterData.copy())
                del FilterData
               
                
                df_cleanup.drop(columns=['Correlation', 'Scale'], inplace=True)
                df_cleanup.columns = ['DataType', 'percent_unique', 'percent_missing', 'Outlier_Data', 'Balanced', \
                                      'IsSkewed', 'ImBalanced_col', 'OrdinalNominal', 'Binning_Values', \
                                      'Data_Quality_Score', 'ProblemType', 'Q_Info']
                
                if pageInfo != 'DataCleanUpAddFeature':
                    for col in Quality_df.index:
                        if col not in Addfeaturecolumns:
                            dtype[col] = DtypeDict[col]
                            scale[col] = ScaleDict[col]
                            for param in df_cleanup.columns:
                                df_cleanup[param][col] = Quality_df[param][col]
                                if param =='DataType' and df_cleanup[param][col]!=typeDict[col] and df_cleanup[param][col]!= 'Id' and df_cleanup[param][col]!= 'Text':
                                    typeDict[col] = df_cleanup[param][col]
                else:
                    for col in Quality_df.index:
                        if col in Addfeaturecolumns:
                            dtype[col] = DtypeDict[col]
                            scale[col] = ScaleDict[col]
                            for param in df_cleanup.columns:
                                df_cleanup[param][col] = Quality_df[param][col]
                                if param =='DataType' and df_cleanup[param][col]!=typeDict[col] and df_cleanup[param][col]!= 'Id':
                                    typeDict[col] = df_cleanup[param][col]
                del Quality_df
                for col in typeDict:
                    if typeDict[col] == 'datetime64[ns, UTC]':
                        typeDict[col] = 'datetime64[ns]'   
                df_cleanup['column_name'] = list(df_cleanup.index)
                df_cleanup['CorelatedWith'] = corelated_Series
                del corelated_Series
                UserInputColumns = list(set(UserInputColumns).difference(set(removedcols)))
                if len(DateFormat) > 0:
                    DateFormat.update(dateFmts)
                else:
                    DateFormat = dateFmts
                if EnDeRequired :  #EN3333333333333......................
                    ColUniqueValues = EncryptData.EncryptIt(json.dumps(ColUniqueValues))
                if auto_retrain:
                    utils.Update_DataCleanUP_FilteredData(correlationId, ColUniqueValues, typeDict, UserInputColumns_combined,
                                                      DateFormat, userId,removedcols = removedcols)
                else:
                    utils.Update_DataCleanUP_FilteredData(correlationId, ColUniqueValues, typeDict, UserInputColumns_combined,
                                                      DateFormat, userId)
    
                utils.logger(logger, correlationId, 'INFO',
                             ('Data_quality_check executed'),str(None))
                if pageInfo != 'DataCleanUpAddFeature':
                   utils.updQdb(correlationId, 'P', '70', pageInfo, userId)    
                feature_name = utils.data_cleanup_json(df_cleanup, dtype, scale)
                feature_name_parent = {}
                feature_name_new ={}
                UserInputColumns = list(set(UserInputColumns) - set(Addfeaturecolumns))
                for key in feature_name:
                    if key in UserInputColumns:
                        feature_name_parent[key]=feature_name[key]
                        
                    elif key in Addfeaturecolumns:
                        feature_name_new[key]=feature_name[key]
                        for i in feature_name_new[key]['Datatype']:
                            feature_name_new[key]['Datatype'][i] = 'False'
                        if typeDict[key] != 'object':
                            feature_name_new[key]['Datatype'][typeDict[key]] = 'True'
                        else:
                            feature_name_new[key]['Datatype']['Text'] = 'True'
                        feature_name_new[key]["AddFeature"] = "True"
                if f1 == True:  
                
                    feature_name_new = {}
                if pageInfo != 'DataCleanUpAddFeature':    
                    utils.updQdb(correlationId, 'P', '90', pageInfo, userId)
    
                if int(targetType) != 4 or (target_from_ps in Addfeaturecolumns and customtargetvalue):
                    TargetProblemType = df_cleanup.ProblemType[target]
                    if df_cleanup.ProblemType[target] == 0 and not customtargetvalue:
                        TargetProblemType = int(targetType)
                elif ((not customtargetvalue ) or (customtargetvalue and bool(Addfeaturecolumns))) and int(targetType) != 4:
                    TargetProblemType = 0
                else:
                    TargetProblemType = 4
                del df_cleanup
                if not customtargetvalue :
                    feature_name[target]['ProblemType'] = str(TargetProblemType)
                if EnDeRequired :   #EN4feature_name.keys()444444444444444.........................
                    feature_name_parent = EncryptData.EncryptIt(json.dumps(feature_name_parent))
                    feature_name_new = EncryptData.EncryptIt(json.dumps(feature_name_new)) 
                if auto_retrain:
                    utils.Update_DE_DataCleanup(correlationId, unchangedColumns, None, feature_name_parent, Outlier_Dict, corrDict,
                                            str(TargetProblemType), OrginalTypes, userId,feature_name_new)
                else:
                    utils.Update_DE_DataCleanup(correlationId, unchangedColumns, flag, feature_name_parent, Outlier_Dict, corrDict,
                                            str(TargetProblemType), OrginalTypes, userId,feature_name_new)
                utils.setRetrain(correlationId, True)
    
                utils.logger(logger, correlationId, 'INFO',
                             ('Data_quality_check Updated Data quality check results DE_DataCleanup'),str(None))
                if pageInfo != 'DataCleanUpAddFeature':    
                   utils.updQdb(correlationId, 'C', '100', pageInfo, userId)
          
        else:
             raise Exception("Please select custom target attribute and then proceed")
        utils.logger(logger, correlationId, 'INFO', ('updatedata completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))

    except Exception as e:
        if pageInfo != 'DataCleanUpAddFeature':
             utils.updQdb(correlationId, 'E', str(e.args), pageInfo, userId)
        utils.logger(logger, correlationId, 'ERROR', 'Trace',str(None))
    else:
        utils.logger(logger, correlationId, 'INFO',
                     ('Data_quality_check completed successfully'),str(None))




def ViewDataQuality(correlationId, pageInfo, userId, flag):
    try:
        logger = utils.logger('Get', correlationId)
        EnDeRequired = utils.getEncryptionFlag(correlationId)
        utils.updQdb(correlationId, 'P', '20', pageInfo, userId)
        utils.logger(logger, correlationId, 'INFO', (
                   'Data_quality_check Updation initiated for flag= ' + str(
                flag) + " correlation Id :" + str(correlationId) + "Data Quality Check"),str(None))
        utils.logger(logger, correlationId, 'INFO', ('viewdataquality started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))

        # Fetch preprocessed data
        data = utils.data_from_chunks(correlationId, 'DE_PreProcessedData')
        '''CHANGES START HERE'''
        #dbproconn, dbprocollection = utils.open_dbconn("ME_FeatureSelection")
        #data_json = dbprocollection.find({"CorrelationId": correlationId})
        #feature_not_created = data_json[0].get("Feature_Not_Created")
        #features_created = data_json[0].get("Features_Created")
        #encoded_new_feature = data_json[0].get("Map_Encode_New_Feature")

        '''CHANGES END HERE'''
        encoders = {}

        # Fetch data to be encoded from data processing table
        dbproconn, dbprocollection = utils.open_dbconn("DE_DataProcessing")
        data_json = dbprocollection.find({"CorrelationId": correlationId})
        dbproconn.close()

        Data_to_Encode = data_json[0].get('DataEncoding')
        '''CHANGES START HERE'''
        #map_encode_new_feature = {}
        #if len(encoded_new_feature) > 0:
        #    for i in range(len(encoded_new_feature)):
        #        map_encode_new_feature[encoded_new_feature[i]] = {'attribute': 'Nominal', 'encoding': 'Label Encoding',
        #                                                          'ChangeRequest': 'True', 'PChangeRequest': 'False'}
        #Data_to_Encode.update(map_encode_new_feature)

        '''CHANGES END HERE'''
        if len(Data_to_Encode) > 0:
            OHEcols = []
            LEcols = []

            for keys, values in Data_to_Encode.items():
                if values.get('encoding') == 'One Hot Encoding' and (
                        values.get('ChangeRequest') == 'True' and values.get('PChangeRequest') != 'True') and (
                        keys != 'ChangeRequest' and keys != 'PChangeRequest'):
                    OHEcols.append(keys)
                elif values.get('encoding') == 'Label Encoding' and (
                        values.get('ChangeRequest') == 'True' and values.get('PChangeRequest') != 'True') and (
                        keys != 'ChangeRequest' and keys != 'PChangeRequest'):
                    LEcols.append(keys)
            # OHE
            # Fetch Pickle file and encoded columns
            if len(OHEcols) > 0:
                ohem, _, enc_cols, _ = utils.get_pickle_file(correlationId, FileType='OHE')
                enc_cols_new = []
                for i in range(len(enc_cols)):
                    if str(enc_cols[i]).split('_')[0] in OHEcols:
                            enc_cols_new.append(enc_cols[i])
                #encoders.update({'LE': {lencm: Lenc_cols_new}})

                encoders = {'OHE': {ohem: {'EncCols': enc_cols, 'OGCols': OHEcols}}}


            if len(LEcols) > 0:
                lencm, _, Lenc_cols, _ = utils.get_pickle_file(correlationId, FileType='LE')
                Lenc_cols_new = []
                for i in range(len(Lenc_cols)):
                    if str(Lenc_cols[i]).split('_L')[0] in LEcols:
                            Lenc_cols_new.append(str(Lenc_cols[i]).split('_L')[0])
                encoders.update({'LE': {lencm: Lenc_cols}})

            OGData = utils.get_OGDataFrame(data, encoders)
        else:
            OGData = data
        del data 

        typeDict, target, removedcols, UserInputColumns, dateFmts = utils.get_DataCleanUP_FilteredData(correlationId)
        '''CHANGES START HERE'''
        #if len(features_created) > 0:
        #    UserInputColumns = UserInputColumns + features_created
        '''CHANGES END HERE'''
        cols = list(typeDict.keys())
        '''CHANGES START HERE'''
        #if len(features_created) > 0:
        #    cols = cols + features_created
        '''CHANGES END HERE'''

        for col in cols:
            if col not in OGData.columns:
                typeDict.pop(col, None)

        OGData = OGData.astype(typeDict)
        df_cleanup, dtype, scale = utils.get_DataCleanUpView(correlationId)
        df_cleanup = df_cleanup[df_cleanup.index.isin(OGData.columns)]
        utils.logger(logger, correlationId, 'INFO',
                     ('Data_quality_check : Fetched Data from Filtered data'),str(None))

        Quality_df, Outlier_Dict, Skewed_cols, text_cols, target, DtypeDict, ScaleDict, DfModified, \
        corrDict, id_cols, corelated_Series, DateFormat = Data_Quality_Checks(OGData, target, None, None, flag)
        t = OGData.columns
        del OGData
        # unique values
        #            ColUniqueValues,typeDict = getUniqueValues(FilterData.copy())

        df_cleanup.drop(columns=['Correlation', 'Scale'], inplace=True)
        df_cleanup.columns = ['DataType', 'percent_unique', 'percent_missing', 'Outlier_Data', 'Balanced', \
                              'IsSkewed', 'ImBalanced_col', 'OrdinalNominal', 'Binning_Values', \
                              'Data_Quality_Score', 'ProblemType', 'Q_Info']

        '''CHANGES START HERE'''
        #if len(features_created) > 0:
        #    for item in features_created:
        #        data = Quality_df.loc[item, df_cleanup.columns]
        #        df_cleanup = df_cleanup.append(data)
        '''CHANGES END HERE'''
        # updating quality df and DtypeDict with new changes
        for col in Quality_df.index:
            for param in df_cleanup.columns:
                df_cleanup[param][col] = Quality_df[param][col]

        df_cleanup['column_name'] = list(df_cleanup.index)
        df_cleanup['CorelatedWith'] = corelated_Series

        utils.logger(logger, correlationId, 'INFO',
                     ('Data_quality_check :Data quality check executed'),str(None))
        utils.updQdb(correlationId, 'P', '70', pageInfo, userId)

        feature_name = utils.data_cleanup_json(df_cleanup, dtype, scale)
        try:
            dbproconn,dbprocollection = utils.open_dbconn("DE_AddNewFeature")
            data_json = list(dbprocollection.find({"CorrelationId" :correlationId}))
            dbproconn.close()
            if 'Features_Created' in data_json[0]:
                add_features = data_json[0]['Features_Created']
            else:
                add_features = []
        except:
            add_features = []
        try:
            if add_features != []:
                df_cleanup_addfeature, dtype_addfeature, scale_addfeature = utils.get_DataCleanUpView_addfeature(correlationId)
                df_cleanup_addfeature = df_cleanup_addfeature[df_cleanup_addfeature.index.isin(t)]
                df_cleanup_addfeature.drop(columns=['Correlation', 'Scale','AddFeature'], inplace=True)
                df_cleanup_addfeature.columns = ['DataType', 'percent_unique', 'percent_missing', 'Outlier_Data', 'Balanced', \
                                      'IsSkewed', 'ImBalanced_col', 'OrdinalNominal', 'Binning_Values', \
                                      'Data_Quality_Score', 'ProblemType', 'Q_Info']
        
                df_cleanup_addfeature['column_name'] = list(df_cleanup_addfeature.index)
                df_cleanup_addfeature['CorelatedWith'] = corelated_Series
                for i in range(len(df_cleanup_addfeature['column_name'])):
                    df_cleanup_addfeature['column_name'][i] = str(df_cleanup_addfeature['column_name'][i]).split('_L')[0]
                #print("df_columns",df_cleanup_addfeature['column_name'])
                #print("columns",dtype_addfeature)
                feature_name_addfeature = utils.data_cleanup_json(df_cleanup_addfeature, dtype_addfeature, scale_addfeature)
                feature_name.update(feature_name_addfeature)
        except:
            utils.logger(logger, correlationId, 'INFO',("Feature name Data_quality_check"),str(None))
        if EnDeRequired :
            feature_name = EncryptData.EncryptIt(json.dumps(feature_name))
        utils.updQdb(correlationId, 'P', '90', pageInfo, userId)
        dbconn, dbcollection = utils.open_dbconn('DE_DataCleanup')
        dbcollection.update_many({"CorrelationId": correlationId}, {'$set': {"ViewDataQuality": feature_name}})
        dbconn.close()

        utils.logger(logger, correlationId, 'INFO',
                     ('Data_quality_check :Updated Data quality check results DE_DataCleanup'),str(None))
        utils.logger(logger, correlationId, 'INFO', ('viewdataquality completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))

        utils.updQdb(correlationId, 'C', '100', pageInfo, userId)
    except Exception as e:
        utils.updQdb(correlationId, 'E', str(e.args), pageInfo, userId)
        utils.logger(logger, correlationId, 'ERROR', 'Trace',str(None))
        utils.save_Py_Logs(logger, correlationId)
    #        return jsonify({'status': 'false', 'message':str(e.args)}), 200
    else:
        utils.logger(logger, correlationId, 'INFO',
                     ('Data_quality_check : Data quality check completed successfully'),str(None))
        utils.save_Py_Logs(logger, correlationId)

def calldatamodification(correlationId):
    dbconn, dbcollection = utils.open_dbconn('DE_DataCleanup')
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    if len(data_json) > 0:
        featurename = data_json[0]['Feature Name']
        EnDeRequired = utils.getEncryptionFlag(correlationId)
        if EnDeRequired:
            import base64
            x = base64.b64decode(featurename) #En.............................
            featurename = json.loads(EncryptData.DescryptIt(x))
        columnBinning = {}
        prescriptionData = {}
        recommendedColumns = {}
        for key,value in featurename.items():
            fields = {}
            removeImbalancedColumns = {}
            ImBalanced = featurename[key]['ImBalanced']
            if ImBalanced == "1":
                recommendation = "Binning to be applied for the imbalanced column "+key+" for categorical values which have low % values"
                Binning_values = featurename[key]['BinningValues']
                updt_dict = {"Binning" : "False","NewName" : "False"}
                for key1,value1 in Binning_values.items():
                    Binning_values[key1].update(updt_dict)
                columnBinning[key] = Binning_values
                columnBinning[key].update({"ChangeRequest" : {
                					"ChangeRequest" : ""
                				},
                				"PChangeRequest" : {
                					"PChangeRequest" : ""
                				}})
            elif ImBalanced == "2":
                removeImbalancedColumns[key] = "column "+key+" id imbalance column and contain only two classes so please drop the column "
            elif ImBalanced == '3':
                prescriptionData[key] = "Target column "+key+" is imbalanced and should be improved using the SMOTE technique"
            outValue = featurename[key]['Outlier']
            #print("outValue",outValue,key,featurename[key]['Outlier'])
            outlier = {}
            if int(float(outValue)) > 0:
                outlier['Text'] = "Column "+key+" contains "+str(outValue)+" outlier values. Replace outlier using Mean, Median, Mode or Custom Value"
                outlier["Mean"] = "False"
                outlier["Median"] = "False"
                outlier["Mode"] = "False"
                outlier["CustomValue"] = ""
                outlier["ChangeRequest"] = ""
                outlier["PChangeRequest"] = ""
            skeweness = {}
            skewValue = featurename[key]["Skewness"] 
            if skewValue == "Yes":
                list1 = list(key)
                key = ''.join(list1)
                skeweness["Skeweness"] = "Columns "+key+" is highly skewed , choose standardization or normalization,log to remove skewness"
                skeweness["BoxCox"] = "False"
                skeweness["Reciprocal"] = "False"
                skeweness["Log"] = "False"
                skeweness["ChangeRequest"] = ""
                skeweness["PChangeRequest"] = ""
            if outlier != {}:
                fields["Outlier"] = outlier
            if skeweness != {}:
                fields["Skewness"] = skeweness
            if removeImbalancedColumns != {}:
                fields['RemoveColumn'] = removeImbalancedColumns
            if fields != {}:
                recommendedColumns[key] = fields

        #print("calldatamodification done")				
    return recommendedColumns,columnBinning,prescriptionData



def GetDataEncodingValues(correlationId):
    dbconn, dbcollection = utils.open_dbconn('DE_DataCleanup')
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    if len(data_json) > 0:
        featurename = data_json[0]['Feature Name']
        EnDeRequired = utils.getEncryptionFlag(correlationId)
        if EnDeRequired:
            import base64
            x = base64.b64decode(featurename) #En.............................
            featurename = json.loads(EncryptData.DescryptIt(x))
        category_list = []
        encodingData = {}
        for key,value in featurename.items():
            datatype = featurename[key]['Datatype']
            if datatype['category'] == 'True':
                category_list.append(key)
                dataEncodingData = {}
                if featurename[key]['Scale'] != {}:
                    dataEncodingData["attribute"] = "Nominal"
                    dataEncodingData["encoding"] = "Label Encoding"
                    dataEncodingData["ChangeRequest"] = "True"
                    dataEncodingData["PChangeRequest"] = ""
                    encodingData[key] = dataEncodingData
    return encodingData,category_list

def GetMissingAndFiltersData(correlationId,ColumnUniqueValues,target_variable,category_list,missingColumns,num_list,templateusecaseid):
        isFilterApplied = "false"
        isChangeRequest = "false"
        dbconn, dbcollection = utils.open_dbconn('DE_DataProcessing')
        data_json = list(dbcollection.find({"CorrelationId": templateusecaseid}))
        if len(data_json) > 0:
            isTemplateDbEncrypted = utils.getEncryptionFlag(templateusecaseid)
            if isTemplateDbEncrypted:
                import base64
                t = base64.b64decode(data_json[0]['Filters'])
                data_json[0]['Filters'] =  eval(EncryptData.DescryptIt(t))
            filters = {}
            categoricalDictionary = {}
            for col in category_list:
                isrequest = False
                for i in ColumnUniqueValues[col]:
                    filters[col] = dict(zip(ColumnUniqueValues[col],["false"]*len(ColumnUniqueValues[col])))
                    if col in data_json[0]['Filters']:
                        for key,value in data_json[0]['Filters'][col].items():
                            if value == "True":
                                    isrequest = True
                                    filters[col][key] = data_json[0]['Filters'][col][key]
                if isrequest:
                    filters[col]['ChangeRequest'] = "True"
                else:
                    filters[col]['ChangeRequest'] = ""
                filters[col]['PChangeRequest'] = ""
        else:
            filters = {}

        MissingValues = {}
        for col in missingColumns:
            for i in ColumnUniqueValues[col]:
                MissingValues[col] = dict(zip(ColumnUniqueValues[col],["false"]*len(ColumnUniqueValues[col])))
            MissingValues[col]['ChangeRequest'] = "True"
            MissingValues[col]['PChangeRequest'] = "False"
            MissingValues[col]['CustomValue'] = None
            MissingValues[col]['CustomFlag'] = "True"

        dataNumerical = {}
        for col in num_list:
            for i in ColumnUniqueValues[col]:
                dataNumerical[col] = {}
                dataNumerical[col]['Mean'] = 'True'
                dataNumerical[col]['Median'] = 'Mode'
                dataNumerical[col]['Mode'] = 'Mode'
                dataNumerical[col]['CustomValue'] = ""
                dataNumerical[col]['ChangeRequest'] = 'True'
                dataNumerical[col]['PChangeRequest'] = ""

        return filters, MissingValues, dataNumerical

def getallcolumns(correlationId):
    category_list = []
    num_list = []
    text_list = []
    date_list = []
    missingcolumns = []
    dbconn, dbcollection = utils.open_dbconn('DE_DataCleanup')
    data_json = list(dbcollection.find({"CorrelationId": correlationId}))
    if len(data_json) > 0:
        featurename = data_json[0]['Feature Name']
        EnDeRequired = utils.getEncryptionFlag(correlationId)
        if EnDeRequired:
            import base64
            x = base64.b64decode(featurename) #En.............................
            featurename = json.loads(EncryptData.DescryptIt(x))
    for key,value in featurename.items():
        datatype = featurename[key]['Datatype']
        if datatype['category'] == 'True':
            if float(featurename[key]["Missing Values"]) > 0:
                missingcolumns.append(key)
            category_list.append(key)
        elif datatype['float64'] == 'True' or datatype['int64'] == 'True':
            if float(featurename[key]["Missing Values"]) > 0:
                missingcolumns.append(key)
            num_list.append(key)
        elif datatype['Text'] == 'True':
            text_list.append(key)
        elif datatype['datetime64[ns]'] == 'True':
            date_list.append(key)
    return category_list, num_list, text_list, date_list, missingcolumns


def UpdateTextDataProcessing(correlationId,templateusecaseid,text_list):
    EnDeRequired_t = utils.getEncryptionFlag(templateusecaseid)
    dbconn, dbcollection = utils.open_dbconn("DE_DataProcessing")
    data_json = list(dbcollection.find({"CorrelationId": templateusecaseid}))
    dbconn.close()
    if EnDeRequired_t:             
        x = base64.b64decode(data_json[0]['DataModification']) #En.............................
        data_json[0]['DataModification'] = json.loads(EncryptData.DescryptIt(x)) 
    text_data = data_json[0]['DataModification']['TextDataPreprocessing']
    if text_data == "":
        text_data = {}
        for col in text_list:
            text_data[col]['Lemmitize'] = 'True'
            text_data[col]['Stemming'] = 'False'
            text_data[col]['Pos'] = 'False'
            text_data[col]['Stopwords'] = []
            text_data[col]['Least_Frequent'] = 0
            text_data[col]['Most_Frequent'] = 0
        text_data['Feature_Generator'] = "Count Vectorizer"
        text_data['Ngrams'] = [3,3]
        text_data['NumberOfCluster'] = 1
        text_data['TextColumnsDeletedByUser'] = []
    return text_data


def main(correlationId, pageInfo, userId, flag = None):
    try:
        logger = utils.logger('Get', correlationId)
        EnDeRequired = utils.getEncryptionFlag(correlationId)
        if pageInfo != 'DataCleanUpAddFeature':
            utils.updQdb(correlationId, 'P', '10', pageInfo, userId)
        utils.logger(logger, correlationId, 'INFO', (
                    'Data_quality_check : Process initiated for '+ str(
                pageInfo) + " correlation Id :" + str(correlationId) + "Data Quality Check"),str(None))
        utils.logger(logger, correlationId, 'INFO', ('custom changes started at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))

        ####### CUSTOM CASCADE CHANGES START HERE #######
        dbconn, dbcollection = utils.open_dbconn("SSAI_DeployedModels")
        data_json = list(dbcollection.find({"CorrelationId": correlationId}))
        dbconn.close()
        if 'DataCurationName' in data_json[0]:
            datacuration_name = data_json[0]["DataCurationName"]
            if datacuration_name not in ["Cascade","Ingrain"]:
                datacuration_name = "Ingrain" 
        else:
            datacuration_name = "Ingrain"
            
        if datacuration_name == "Cascade":
            cascadeMergedDf = appendCascadeDf(correlationId)
            
            utils.updateIngestDataWithCascade(cascadeMergedDf, correlationId, pageInfo, userId)
            del cascadeMergedDf
            
        ####### CUSTOM CASCADE CHANGES END HERE #######
        utils.logger(logger, correlationId, 'INFO', ('custom changes Completed at '  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(None))
        
        dbconn, dbcollection = utils.open_dbconn("PS_BusinessProblem")
        data_json = list(dbcollection.find({"CorrelationId": correlationId}))
        dbconn.close()
        target_variable = data_json[0]['TargetColumn']
        if 'IsCustomColumnSelected' in data_json[0]:
            customtargetvalue = data_json[0]['IsCustomColumnSelected']
            customtargetvalue = eval(customtargetvalue)
        else:
            customtargetvalue = False
        if 'ParentCorrelationId' in data_json[0] and (data_json[0]['ParentCorrelationId']) != None:
            parentcorrelationId = data_json[0]['ParentCorrelationId']
        elif "TemplateUsecaseID" in data_json[0] and (data_json[0]['TemplateUsecaseID']) != None:
            parentcorrelationId = data_json[0]['TemplateUsecaseID']
        else:
            parentcorrelationId = ''
        #parentcorrelationId = ''
        if not customtargetvalue:
            UniqueIdentifir = data_json[0]['TargetUniqueIdentifier']
            if data_json[0]['ProblemType'] != "TimeSeries" and data_json[0]['ProblemType'] != "Regression":
                UserInputColumns = data_json[0]['InputColumns']
                Prob_type = None
            elif data_json[0]['ProblemType'] == "Regression":   
                UserInputColumns = data_json[0]['InputColumns']
                Prob_type =  data_json[0]['ProblemType']
            else:
                UserInputColumns = [data_json[0]['TimeSeries']['TimeSeriesColumn']]
                Prob_type = data_json[0]['ProblemType']

            utils.logger(logger, correlationId, 'INFO', (
                    'Data_quality_check : page info:  ' + str(pageInfo) + " Target :" + str(
                target_variable) + " Input Columns :" + str(UserInputColumns) +" from PS_BusinessProblem"),str(None))
        # Adding target column filter dafta for all column from base data
            UserInputColumns.insert(0, target_variable)
            if UniqueIdentifir != None:
                UserInputColumns.insert(0, UniqueIdentifir)
            if pageInfo != 'DataCleanUpAddFeature':
                utils.updQdb(correlationId, 'P', '20', pageInfo, userId)
            offlineutility = utils.checkofflineutility(correlationId)
            if offlineutility:
                data_t = utils.data_from_chunks_offline_utility(corid=correlationId, collection="DataSet_IngestData",lime=None, recent=None,DataSetUId=offlineutility)
                message=unaccepted_datatype(data_t[UserInputColumns])
                if type(message)==str:
                    utils.updQdb(correlationId,'E',message,pageInfo,userId)
                    return
            else:
                data_t = utils.data_from_chunks(corid=correlationId, collection="PS_IngestedData")
                message=unaccepted_datatype(data_t[UserInputColumns])
                if type(message)==str:
                    utils.updQdb(correlationId,'E',message,pageInfo,userId)
                    return
            ###############changes for visualization ##########################################
            dbconn1, dbcollection1 = utils.open_dbconn("PS_IngestedData")
            data_json1 = list(dbcollection1.find({"CorrelationId": correlationId}))
            dbconn1.close()            
            previousLastDate = data_json1[0].get("previousLastDate")
            if previousLastDate:
                previousLastDate = pd.to_datetime(previousLastDate).tz_localize(None)
                
#                ps_data = data_t.copy()
                tmp = list(data_t["DateColumn"])
                data_t["DateColumn"] = [i.tz_localize(None) for i in tmp]
                del tmp
                filtered_ps_data = data_t[(data_t["DateColumn"])>(previousLastDate)]
                uid_list = filtered_ps_data[UniqueIdentifir]
                uid_list = list(uid_list.unique())
                ignore_fake_indices = ["\"\"","None",None,"\" \"",np.float("nan"),"nan"]
                uid_list = [i for i in uid_list if i not in ignore_fake_indices]
              
                id_ps_data = data_json[0]["_id"]
                dbcollection.update_one({"_id": id_ps_data},{"$set":{"delta_uids":uid_list}})
                #dbconn.close()
            #############################################################

            data_t = data_t[UserInputColumns]
            utils.logger(logger, correlationId, 'INFO',
                     ('Data_quality_check : Fetched Data from PS_IngestedData'),str(None))
            Addfeaturecolumns = []
        else:
            UniqueIdentifir = data_json[0]['TargetUniqueIdentifier']
            if data_json[0]['ProblemType'] != "TimeSeries" and data_json[0]['ProblemType'] != "Regression":
                UserInputColumns = data_json[0]['InputColumns']
                Prob_type = None
            elif data_json[0]['ProblemType'] == "Regression":   
                UserInputColumns = data_json[0]['InputColumns']
                Prob_type =  data_json[0]['ProblemType']
            else:
                UserInputColumns = [data_json[0]['TimeSeries']['TimeSeriesColumn']]
                Prob_type = data_json[0]['ProblemType']
        
            utils.logger(logger, correlationId, 'INFO', (
                            'Data_quality_check : page info:  ' + str(pageInfo) + " Target :" + str(
                        target_variable) + " Input Columns :" + str(UserInputColumns)  + " from PS_BusinessProblem"),str(None))
        
                
            if UniqueIdentifir != None:
                UserInputColumns.insert(0, UniqueIdentifir)
        
                # Fetch data
            if pageInfo != 'DataCleanUpAddFeature':
                utils.updQdb(correlationId, 'P', '20', pageInfo, userId)
            offlineutility = utils.checkofflineutility(correlationId)
            if offlineutility:
                data_t = utils.data_from_chunks_offline_utility(corid=correlationId, collection="DataSet_IngestData",lime=None, recent=None,DataSetUId=offlineutility)
                message=unaccepted_datatype(data_t[UserInputColumns])
                if type(message)==str:
                    utils.updQdb(correlationId,'E',message,pageInfo,userId)
                    return
            else:
                data_t = utils.data_from_chunks(corid=correlationId, collection="PS_IngestedData") 
                message=unaccepted_datatype(data_t[UserInputColumns])
                if type(message)==str:
                    utils.updQdb(correlationId,'E',message,pageInfo,userId)
                    return
            
            ###############changes for visualization ##########################################
            dbconn1, dbcollection1 = utils.open_dbconn("PS_IngestedData")
            data_json1 = list(dbcollection1.find({"CorrelationId": correlationId}))
            dbconn1.close()            
            previousLastDate = data_json1[0].get("previousLastDate")
            if previousLastDate:
                previousLastDate = pd.to_datetime(previousLastDate).tz_localize(None)
                
#                ps_data = data_t.copy()
                tmp = list(data_t["DateColumn"])
                data_t["DateColumn"] = [i.tz_localize(None) for i in tmp]
                filtered_ps_data = data_t[(data_t["DateColumn"])>(previousLastDate)]
                uid_list = filtered_ps_data[UniqueIdentifir]
                uid_list = list(uid_list.unique())
                ignore_fake_indices = ["\"\"","None",None,"\" \"",np.float("nan"),"nan"]
                uid_list = [i for i in uid_list if i not in ignore_fake_indices]
               
                id_ps_data = data_json[0]["_id"]
                dbcollection.update_one({"_id": id_ps_data},{"$set":{"delta_uids":uid_list}})
                #dbconn.close()
            #############################################################
                
            data_t = data_t[UserInputColumns] 
            Addfeaturecolumns = []
        # Data Clean up
        if pageInfo != 'DataCleanUpAddFeature' :
           utils.updQdb(correlationId, 'P', '50', pageInfo, userId)

        ## changes for prediction visualization
                
        
        ##############################################
        # Data_quality_df,Outlier_Dict,Skewed_cols,text_cols,target,DtypeDict,ScaleDict,data_copy,corrDict = Data_Quality_Checks(ingedata.copy(),target)

        Data_quality_df, Outlier_Dict, Skewed_cols, text_cols, target, DtypeDict, \
        ScaleDict, data_t, corrDict, id_cols, corelated_Series, DateFormat = Data_Quality_Checks(
            data_t[UserInputColumns], target_variable, Prob_type,customtargetvalue=customtargetvalue,Addfeaturecolumns=Addfeaturecolumns)
        Data_quality_df['CorelatedWith'] = corelated_Series
        del corelated_Series
        ColUniqueValues, typeDict = getUniqueValues(data_t)
        msg = "Successfully Processed "+str(data_t.shape[0])+" records."
        del data_t
        if pageInfo != 'DataCleanUpAddFeature':
            utils.updQdb(correlationId, 'P', '70', pageInfo, userId)
        #        data_copy = checkDateNull(data_copy)
        OrgDtypes = Data_quality_df.originalDtyps.to_dict()
        if ((not customtargetvalue ) or (customtargetvalue and bool(Addfeaturecolumns))):
            if data_json[0]['ProblemType'] != "TimeSeries" and data_json[0]['ProblemType'] != "Regression":
                TargetProblemType = Data_quality_df.ProblemType[target]
            elif data_json[0]['ProblemType'] == "Regression":
                TargetProblemType = 1
            else:
            
                TargetProblemType = 4
        else:
            TargetProblemType = 0
        
        if EnDeRequired:
            ColUniqueValues = EncryptData.EncryptIt(json.dumps(ColUniqueValues))
        if pageInfo != 'DataCleanUpAddFeature':
            utils.save_DataCleanUP_FilteredData(target_variable, correlationId, ColUniqueValues, typeDict, UserInputColumns,
                                            DateFormat, userId)
        else:
            dbproconn,dbprocollection = utils.open_dbconn("DataCleanUP_FilteredData")
            data_json = list(dbprocollection.find({"CorrelationId" :correlationId}))
            dbproconn.close()
            new_dict = data_json[0]['types']
            dbproconn,dbprocollection = utils.open_dbconn("DE_AddNewFeature")
            data_json2 = list(dbprocollection.find({"CorrelationId" :correlationId}))
            dbproconn.close() 
            Features_Created = data_json2[0]['Features_Created']
            feature_present = False
            for i in range(len(Features_Created)):
                if Features_Created[i] in new_dict:
                    feature_present = True
            if feature_present:
                typeDict = {}                                  
                for i in range(len(UserInputColumns)):
                    typeDict[UserInputColumns[i]] = new_dict[UserInputColumns[i]]            
            utils.Update_DataCleanUP_FilteredData(correlationId, ColUniqueValues, typeDict, UserInputColumns,
                                                          DateFormat, userId)

        utils.logger(logger, correlationId, 'INFO',
                     ('Data_quality_check : Data quality check executed'),str(None))
        if pageInfo != 'DataCleanUpAddFeature':
            utils.updQdb(correlationId, 'P', '90', pageInfo, userId)

        feature_name = utils.data_cleanup_json(Data_quality_df, DtypeDict, ScaleDict)
        del Data_quality_df
        if EnDeRequired :
            feature_name = EncryptData.EncryptIt(json.dumps(feature_name))
        
        if pageInfo != 'DataCleanUpAddFeature':
            utils.save_DE_DataCleanup(feature_name, Outlier_Dict, corrDict, correlationId, str(TargetProblemType),
                                  OrgDtypes, userId,msg)
        else:
            dbproconn,dbprocollection = utils.open_dbconn("DE_DataCleanup")
            data_json2 = list(dbprocollection.find({"CorrelationId" :correlationId}))
            dbproconn.close() 
            feature_name = data_json2[0]['Feature Name']
            utils.Update_DE_DataCleanup2(correlationId, flag=None, feature_name=feature_name, Outlier_Dict=Outlier_Dict, corrDict=corrDict,
                                            TargetProblemType=str(TargetProblemType),  userId=userId)

            UpdateData(correlationId, pageInfo, userId, flag=None)
        if parentcorrelationId != '' and pageInfo != 'DataCleanUpAddFeature':
            from datapreprocessing import Add_Features
            utils.getAddFeatures(UserInputColumns,parentcorrelationId,correlationId)
            invokeAddFeature = {}
            Add_Features.main(invokeAddFeature,correlationId,pageInfo, userId)
            FilterData,typeDict  = combine_data(correlationId,typeDict)
            Data_quality_df, Outlier_Dict, Skewed_cols, text_cols, target, DtypeDict, \
            ScaleDict, FilterData, corrDict, id_cols, corelated_Series, DateFormat = Data_Quality_Checks(
                FilterData, target_variable, Prob_type,customtargetvalue=customtargetvalue,Addfeaturecolumns=Addfeaturecolumns)
            Data_quality_df['CorelatedWith'] = corelated_Series
            del corelated_Series
            ColUniqueValues, typeDict = getUniqueValues(FilterData)
            msg = "Successfully Processed "+str(FilterData.shape[0])+" records."
            del FilterData
            if pageInfo != 'DataCleanUpAddFeature':
                utils.updQdb(correlationId, 'P', '70', pageInfo, userId)
            #        data_copy = checkDateNull(data_copy)
            OrgDtypes = Data_quality_df.originalDtyps.to_dict()
            if ((not customtargetvalue ) or (customtargetvalue and bool(Addfeaturecolumns))):
                if data_json[0]['ProblemType'] != "TimeSeries" and data_json[0]['ProblemType'] != "Regression":
                    TargetProblemType = Data_quality_df.ProblemType[target]
                elif data_json[0]['ProblemType'] == "Regression":
                    TargetProblemType = 1
                else:
                    #            if OrgDtypes[target] in num_type:
                    #                 Data_quality_df.DataType[target] = 'float64'
                    #                 typeDict[target] = 'float64'
                    #                 DtypeDict[target] = [ 'float64', 'category','int64', 'Id']
                    TargetProblemType = 4
            else:
                TargetProblemType = 0
            if EnDeRequired:
                ColUniqueValues = EncryptData.EncryptIt(json.dumps(ColUniqueValues))
            utils.Update_DataCleanUP_FilteredData(correlationId, ColUniqueValues, typeDict, UserInputColumns,
                                                          DateFormat, userId)
            utils.logger(logger, correlationId, 'INFO',
                         ('Data_quality_check : Data quality check executed'),str(None))
            if pageInfo != 'DataCleanUpAddFeature':
                utils.updQdb(correlationId, 'P', '90', pageInfo, userId)
    
            feature_name = utils.data_cleanup_json(Data_quality_df, DtypeDict, ScaleDict)
            del Data_quality_df
            feature_name_parent = {}
            feature_name_new ={}
            UserInputColumns = list(set(UserInputColumns) - set(Addfeaturecolumns))
            for key in feature_name:
                if key in UserInputColumns:
                    feature_name_parent[key]=feature_name[key]
                            
                elif key in Addfeaturecolumns:
                    feature_name_new[key]=feature_name[key]
                    
                    feature_name_new[key]["AddFeature"] = "True"
            if EnDeRequired :
                feature_name_parent = EncryptData.EncryptIt(json.dumps(feature_name_parent))
                feature_name_new = EncryptData.EncryptIt(json.dumps(feature_name_new))
            
            utils.Update_DE_DataCleanup2(correlationId, flag=None, feature_name=feature_name_parent, Outlier_Dict=Outlier_Dict, corrDict=corrDict,
                                                TargetProblemType=str(TargetProblemType),userId=userId,feature_name_new=feature_name_new)
            UpdateData(correlationId, pageInfo, userId, flag=None)
        utils.logger(logger, correlationId, 'INFO',
                     ('Data_quality_check : Saved Data quality check results DE_DataCleanup'),str(None))

        if pageInfo != 'DataCleanUpAddFeature':
            utils.updQdb(correlationId, 'C', '100', pageInfo, userId)
        if flag == 'AutoTrain':
            #print("inside for loop")
            dbconn, dbcollection = utils.open_dbconn("PS_BusinessProblem")
            data_json = list(dbcollection.find({"CorrelationId": correlationId}))
            dbconn.close()
            if "TemplateUsecaseID" in data_json[0]:
                templateusecaseid_parent = data_json[0]['TemplateUsecaseID']
                dbconn, dbcollection = utils.open_dbconn('DE_DataCleanup')
                data_json = list(dbcollection.find({"CorrelationId": templateusecaseid_parent}))
                dbconn.close()
                if len(data_json) >0:
                    if ('DtypeModifiedColumns' in  data_json[0] and data_json[0]['DtypeModifiedColumns']!={})  or ('ScaleModifiedColumns' in data_json[0] and data_json[0]['ScaleModifiedColumns']!={}):
                        #if dtypemodifiedcolumns present in template updatedatacleanup call is required
                        dbconn, dbcollection = utils.open_dbconn('DE_DataCleanup')
                        data_child = list(dbcollection.find({"CorrelationId": correlationId}))
                        if ('DtypeModifiedColumns' in  data_json[0] and data_json[0]['DtypeModifiedColumns']!={}):
                            data_child[0]['DtypeModifiedColumns'] = data_json[0]['DtypeModifiedColumns']
                        else:
                            data_child[0]['DtypeModifiedColumns'] = {}
                        if ('ScaleModifiedColumns' in data_json[0] and data_json[0]['ScaleModifiedColumns']!={}):
                            data_child[0]['ScaleModifiedColumns'] = data_json[0]['ScaleModifiedColumns']
                        else:
                            data_child[0]['ScaleModifiedColumns'] = {}
			
                        dbcollection.update_one({"CorrelationId":correlationId},{'$set':{         
                                    'DtypeModifiedColumns' : data_child[0]['DtypeModifiedColumns'],
                                    'ScaleModifiedColumns' : data_child[0]['ScaleModifiedColumns']
                                   }}) 
                        dbconn.close()
                        dbconn, dbcollection = utils.open_dbconn("SSAI_IngrainRequests")
                        data_cleanup = list(dbcollection.find({"CorrelationId": correlationId, "pageInfo": "DataCleanUp"}))
                        import uuid
                        dbcollection.insert({
                            "_id": str(uuid.uuid4()),
                            "CorrelationId":correlationId,
                            "RequestId":str(uuid.uuid4()),
                            "ProcessId":data_cleanup[0].get('ProcessId'),
                            "Status":"P",
                            "ModelName":"null",
                            "RequestStatus":"In - Progress",
                            "RetryCount":0,
                            "ProblemType":"null",
                            "Message":"null",
                            "UniId":data_cleanup[0].get('UniId'),
                            "Progress":"In - Progress",
                            "pageInfo":"UpdateDataCleanUp",
                            "ParamArgs":"{}",
                            "Function":"DataCleanUp",
                            "CreatedByUser":data_cleanup[0].get('CreatedByUser'),
                            "CreatedOn":str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                            "ModifiedByUser":data_cleanup[0].get('ModifiedByUser'),
                            "ModifiedOn":str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                            "LastProcessedOn":"null",
                            "AppID":data_cleanup[0].get('AppID'),
                            "ClientId":data_cleanup[0].get('ClientId'),
                            "DeliveryconstructId":data_cleanup[0].get('DeliveryconstructId'),
                            "TemplateUseCaseID":data_cleanup[0].get('TemplateUseCaseID')
                            })
                        dbconn.close()
                        UpdateData(correlationId,"UpdateDataCleanUp",userId,1)
                ###check if select option present for any column, if yes forcefully assign that to category
                dbconn, dbcollection = utils.open_dbconn('DE_DataCleanup')
                data_json = list(dbcollection.find({"CorrelationId": correlationId}))
                featurename = data_json[0]['Feature Name']
                problemtype = data_json[0]['Target_ProblemType']
                EnDeRequired = utils.getEncryptionFlag(correlationId)
                if EnDeRequired:
                    import base64
                    x = base64.b64decode(featurename) #En.............................
                    featurename = json.loads(EncryptData.DescryptIt(x))
                DtypeModifiedColumns = {}
                check_flag = False
                for key,value in featurename.items():
                    datatype = featurename[key]['Datatype']
                    if 'Select Option' in datatype and datatype["Select Option"] == 'True':
                        check_flag = True
                        DtypeModifiedColumns[key] = 'category'
                        datatype["Select Option"] = 'False'
                        datatype['category'] = 'True'
                if check_flag:
                    if EnDeRequired:
                        featurename = EncryptData.EncryptIt(json.dumps(featurename))
                    dbcollection.update_one({"CorrelationId":correlationId},{'$set':{         
                                'DtypeModifiedColumns' : DtypeModifiedColumns,
                                'Feature Name' : featurename
                               }})
                    dbconn.close()
                    UpdateData(correlationId,"UpdateDataCleanUp",userId,1)
                recommendedColumns,columnBinning,prescriptionData = calldatamodification(correlationId)
                encodingData,category_list = GetDataEncodingValues(correlationId)
                dbconn, dbcollection = utils.open_dbconn('DataCleanUP_FilteredData')
                data_json = list(dbcollection.find({"CorrelationId": correlationId}))
                dbconn.close()
                uniquevalues = data_json[0]["ColumnUniqueValues"]
                if EnDeRequired:
                    import base64
                    x = base64.b64decode(uniquevalues) #En.............................
                    uniquevalues = json.loads(EncryptData.DescryptIt(x)) 
                target_variable = data_json[0]["target_variable"]
                category_list, num_list, text_list, date_list, missingcolumns = getallcolumns(correlationId)
                filters, MissingValues, dataNumerical = GetMissingAndFiltersData(correlationId,uniquevalues,target_variable,category_list,missingcolumns,num_list,templateusecaseid_parent)
                ds = [MissingValues, dataNumerical]
                MissingValues_final = {}
                for k in MissingValues.keys():
                    MissingValues_final[k] = [d[k] for d in ds if k in d][0]

                Smote ={}
                Smote['Flag'] = "False"
                Smote['ChangeRequest'] = "False"
                Smote['PChangeRequest'] = "False"

                DataModification = columnBinning
                DataModification['Features'] = recommendedColumns
                DataModification['Prescriptions'] = prescriptionData
                if problemtype == '4':
                    DataModification['Features'] = {}
                    DataModification['Features']['Interpolation'] = "Linear"
                if target_variable in encodingData and len(encodingData[target_variable]) <= 4:
                    target_variable = "null"
                if templateusecaseid_parent != None:
                    text_data = UpdateTextDataProcessing(correlationId,templateusecaseid_parent,text_list)
                else:
                    text_data ={}
                if text_data != {}:
                    DataModification['TextDataPreprocessing'] = text_data

                if EnDeRequired:
                    DataModification = EncryptData.EncryptIt(json.dumps(DataModification))
                    filters = EncryptData.EncryptIt(json.dumps(filters))
                    MissingValues_final = EncryptData.EncryptIt(json.dumps(MissingValues_final))


                dbconn, dbcollection = utils.open_dbconn("DE_DataProcessing")
                data_json_check = list(dbcollection.find({"CorrelationId":correlationId}))
                if len(data_json_check) > 0:
                     dbcollection.remove({"CorrelationId":correlationId})
                import uuid
                dbcollection.insert({
                    "_id" : str(uuid.uuid4()),
                    "CorrelationId":correlationId,
                    "Flag":"False",
                    "Filters":filters,
                    "MissingValues":MissingValues_final,
                    "DataEncoding":encodingData,
                    "DataModification":DataModification,
                    "TargetColumn":target_variable,
                    "Smote":Smote,
                    "DataTransformationApplied": False,
                    "CreatedOn":str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                    "ModifiedOn":str(datetime.now().strftime('%Y-%m-%d %H:%M:%S'))
                    })
                dbconn.close()
                dbconn, dbcollection = utils.open_dbconn("SSAI_IngrainRequests")
                data_cleanup = list(dbcollection.find({"CorrelationId": correlationId, "pageInfo": "DataCleanUp"}))
                import uuid
                dbcollection.insert({
                        "_id": str(uuid.uuid4()),
                        "CorrelationId":correlationId,
                        "RequestId":str(uuid.uuid4()),
                        "ProcessId":data_cleanup[0].get('ProcessId'),
                        "Status":"P",
                        "ModelName":"null",
                        "RequestStatus":"In - Progress",
                        "RetryCount":0,
                        "ProblemType":"null",
                        "Message":"null",
                        "UniId":data_cleanup[0].get('UniId'),
                        "Progress":"In - Progress",
                        "pageInfo":"DataPreprocessing",
                        "ParamArgs":"{}",
                        "Function":"DataTransform",
                        "CreatedByUser":data_cleanup[0].get('CreatedByUser'),
                        "CreatedOn":str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                        "ModifiedByUser":data_cleanup[0].get('ModifiedByUser'),
                        "ModifiedOn":str(datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                        "LastProcessedOn":"null",
                        "AppID":data_cleanup[0].get('AppID'),
                        "ClientId":data_cleanup[0].get('ClientId'),
                        "DeliveryconstructId":data_cleanup[0].get('DeliveryconstructId'),
                        "TemplateUseCaseID":data_cleanup[0].get('TemplateUseCaseID')
                        })
                dbconn.close()
                try:
                    utils.updateautotrain_record(correlationId,"DataPreprocessing","50")
                except:
                    utils.logger(logger, correlationId, 'INFO',
                     ('No Autotrain'),str(None))

            from datapreprocessing import Data_PreProcessing
            Data_PreProcessing.main(correlationId,"DataPreprocessing",userId,flag = 'AutoTrain')           
    except Exception as e:
        if pageInfo != 'DataCleanUpAddFeature':
            utils.updQdb(correlationId, 'E', e.args[0], pageInfo, userId)
        if flag == 'AutoTrain':
            utils.updateautotrain_record_forerror(correlationId,"DataPreprocessing")
            utils.updQdb(correlationId, 'E', e.args[0], "TrainAndPredict", userId)

        utils.logger(logger, correlationId, 'ERROR', 'Trace',str(None))
        #utils.save_Py_Logs(logger, correlationId)
    #        return jsonify({'status': 'false', 'message':str(e.args)}), 200
    else:
        utils.logger(logger, correlationId, 'INFO',
                     ('Data_quality_check : Data quality check completed successfully'),str(None))
        #utils.save_Py_Logs(logger, correlationId)
#        return jsonify({'status': 'true','message':"success"}), 200

