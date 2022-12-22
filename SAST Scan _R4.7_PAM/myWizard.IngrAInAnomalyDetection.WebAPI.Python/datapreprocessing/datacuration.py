# -*- coding: utf-8 -*-
"""
Created on Mon Aug  8 18:40:07 2022

@author: k.sudhakara.reddy
"""

# importing libraries
import numpy as np
import pandas as pd
import datetime
import re
# from imblearn.over_sampling import SMOTE
import scipy.stats as ss
import itertools
import math
from SSAIutils import utils
from bson import json_util
from SSAIutils import EncryptData
from datetime import datetime,timedelta
import json
import base64

global num_type, text_cols, DATE_FORMATS, ordinal_vals
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







def getUniqueValues(df):
    UniqueValuesCol = {}
    typeDict = {}
    for col in df:
        typeDict[col] = df.dtypes[col].name
        if df[col].dtype.name == 'datetime64[ns]':
            UniqueValuesCol[col] = list(df[col].dropna().unique().astype('str'))
        if df[col].dtype.name in num_type or df[col].dtype.name == 'category':
            UniqueValuesCol[col] = [np.asscalar(np.array([x])) for x in list(df[col].dropna().unique())]
        if df[col].dtype.name == 'object':
            if len(list(df[col].dropna().unique())) > 2:
                UniqueValuesCol[col] = list(df[col].dropna().unique())[:3]
            else:
                UniqueValuesCol[col] = list(df[col].dropna().unique())
    return UniqueValuesCol, typeDict



def identifyDaType(data_copy, Prob_type,target=None):
    # replace any space from feature names
    # data_copy.columns = data_copy.columns.str.replace(' ','')
    logger = utils.logger('Get','Info')
    # get headers of dataframe
    names = data_copy.columns.values.tolist()
    try:
        maxCat = int(1000)
    except:
        maxCat=600
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
    
    """if Prob_type != "TimeSeries":
        uniquePercent = len(data_copy[target].unique()) * 1.0 / len(data_copy[target]) * 100
        if uniquePercent < 10:
            data_copy[target] = data_copy[target].astype('category')
    else:
        try:
            data_copy[target] = data_copy[target].astype('float64')
        except:
            data_copy[target] = data_copy[target].astype('category')"""
    return data_copy, id_cols, date_cols, text_cols, incorrect_date


# ==============================================================================
# correlation
# ==============================================================================


# ==============================================================================
# class imbalance check for target column
# ==============================================================================
# set target column and perform imbalance check


def CheckCorrelation(data_copy, cols_to_remove, target):
    corrDict={}
    corelated_Series=[]            
    return corrDict, pd.Series(corelated_Series)

def Data_Quality_Checks(data, target=None, Prob_type=None, DfModified=None, flag=None, text_cols=None, id_cols=None,
                        OrgnalTypes=None, scale_v=None):
    
    if flag == None:
        empty_cols = [col for col in data.columns if data[col].dropna().empty]
        data.drop(columns=empty_cols, inplace=True)
        OrgnalTypes = pd.Series([data[col].dtype.name for col in data.columns], index=data.columns).to_dict()
        data_copy, id_cols, date_cols, text_cols, incorrect_date = identifyDaType(data.copy(), target, Prob_type)
        cols_to_remove = id_cols + date_cols + text_cols
        if Prob_type == 'TimeSeries':
            OrgnalTypes[target] = data_copy[target].dtype.name
        else:
            corrDict, corelated_Series = CheckCorrelation(data_copy, cols_to_remove, target)
        '''MAKE CHANGE, PASS PROBLEMTYPE'''

    columns = data_copy.columns
    # Recommendations = pd.Series(data = None,index = columns)
    ProblemType = pd.Series(data=0, index=columns)
    # Prescriptions = pd.Series(data = None,index = columns)
    # UnknownType = pd.Series(data = 0,index = columns)
    Data_quality_df = pd.DataFrame({'column_name': columns,
                                    'DataType': data_copy.dtypes,
                                    # 'CorelatedWith' : corelated_Series,
                                    'ProblemType': ProblemType,
                                    'originalDtyps': list(OrgnalTypes.values())
                                    }, index=columns)

    DtypeDict = {}
    ScaleDict = {}
    DateFormat = {}
    Outlier_Dict={}
    # Forming recommendation for each data quality check of columns
    for col in data_copy.columns:
        dtypelist2 = ["category", "float64","int64","datetime64[ns]","Id","Text"]

        if data_copy[col].dtype.name == 'datetime64[ns]':
            DateFormat[col] = 'YYYY-MM-DD HH:MM:SS'

        if col == target and data_copy[col].dtype.name == 'category':
            if len(data_copy[col].dropna().unique()) > 2:
                Data_quality_df.ProblemType[col] = 3
            else:
                Data_quality_df.ProblemType[col] = 2
        elif col == target and data_copy[col].dtype.name in num_type:
            Data_quality_df.ProblemType[col] = 1
        else:
            Data_quality_df.ProblemType[col] = 0
        
        if col in id_cols:
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



    return Data_quality_df, Outlier_Dict, text_cols, target, \
           DtypeDict, ScaleDict, data_copy, corrDict, id_cols, corelated_Series, DateFormat


def checkDateNull(df):
    for col in df.columns:
        if df[col].dtype.name == "datetime64[ns]" and df[col].isnull().sum() > 0:
            df[col] = df[col].astype(str)
    return df



def main(correlationId, pageInfo, userId,requestId,UniId=None,ClientID=None,DCUID=None,ServiceID=None,userdefinedmodelname=None,EnDeRequired=None):
   try:
        Incremental=False
        logger = utils.logger('Get', correlationId)
        EnDeRequired = True
        if EnDeRequired == None or not isinstance(EnDeRequired,bool):
            raise Exception("Encryption Flag is a mandatory field")
        #utils.updQdb(correlationId, 'P', '40', pageInfo, userId,requestId)
        utils.updQdb(correlationId, 'P', '40', pageInfo, userId,requestId=requestId)
        utils.logger(logger, correlationId, 'INFO', ('Fetched Data from PS_IngestedData'  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))
        offlineutility = utils.checkofflineutility(correlationId)
        if offlineutility:
            data_t = utils.data_from_chunks_offline_utility(corid=correlationId, collection="DataSet_IngestData",lime=None, recent=None,DataSetUId=offlineutility)
        else:
            data_t = utils.data_from_chunks(corid=correlationId,collection="PS_IngestedData")  
        utils.logger(logger, correlationId, 'INFO', ('Fetched Data from PS_IngestedData'  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))

        #utils.updQdb(correlationId, 'P', '45', pageInfo, userId,requestId)
        utils.updQdb(correlationId, 'P', '45', pageInfo, userId,requestId=requestId)
        Prob_type =None
        Data_quality_df, Outlier_Dict, text_cols, target, DtypeDict, \
        ScaleDict, data_copy, corrDict, id_cols, corelated_Series, DateFormat = Data_Quality_Checks(
            data_t, Prob_type)
        Data_quality_df['CorelatedWith'] = pd.Series(data=None,index=Data_quality_df.index)
        ColUniqueValues, typeDict = getUniqueValues(data_copy)
        
        #utils.updQdb(correlationId, 'P', '60', pageInfo, userId,requestId)
        utils.updQdb(correlationId, 'P', '60', pageInfo, userId,requestId=requestId)
        OrgDtypes = Data_quality_df.originalDtyps.to_dict()
        
        for key,value in ColUniqueValues.items():
            if isinstance(value,list):
                ColUniqueValues[key] = [str(i) for i in value]
        EnDeRequired = True
                
        if EnDeRequired:
            ColUniqueValues = EncryptData.EncryptIt(json.dumps(ColUniqueValues))
        
        UserInputColumns=[]
        target_variable = None
        utils.save_DataCleanUP_FilteredData(target_variable,correlationId, ColUniqueValues, typeDict, UserInputColumns,
                                            DateFormat, userId)

        utils.logger(logger, correlationId, 'INFO', ('Data quality check executed'  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))

        #utils.updQdb(correlationId, 'P', '90', pageInfo, userId,requestId)
        utils.updQdb(correlationId, 'P', '90', pageInfo, userId,requestId=requestId)
        


        feature_name = utils.data_cleanup_json(Data_quality_df, DtypeDict, ScaleDict)

        if EnDeRequired:
            feature_name = EncryptData.EncryptIt(json.dumps(feature_name,default=json_util.default))
        
        utils.save_DE_DataCleanup(feature_name, Outlier_Dict, corrDict, correlationId, 
                                  OrgDtypes, userId,'DE_DataCleanup')
        
        utils.logger(logger, correlationId, 'INFO', ('saved Data quality check results DE_DataCleanup'  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))),str(requestId))

   except Exception as e:
        
        utils.updQdb(correlationId, 'E', e.args[0], pageInfo, userId,requestId=requestId)
        utils.logger(logger, correlationId, 'ERROR', 'Trace',str(requestId))
        utils.save_Py_Logs(logger, correlationId)
        raise Exception (e.args[0]) 









