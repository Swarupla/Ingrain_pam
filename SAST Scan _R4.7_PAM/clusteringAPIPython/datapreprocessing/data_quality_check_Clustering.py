# -*- coding: utf-8 -*-
"""
Created on Wed Jan 16 11:53:27 2019

@author: s.siddappa.dinnimani
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
from main import EncryptData
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
    newdict = {k: v for k, v in Skew.items() if -2 <= v >= 2}
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
                stmt_qlty.append("- Missing(" + str(Quality_Df.Missing[col]) + ")")
            else:
                num_score.append(0)
                stmt_qlty.append("- Missing(0)")
            if Quality_Df.incorrect_date[col] == 1:
                num_score.append(10)
                stmt_qlty.append("- Incorrect Date Format(10.0)")
            else:
                num_score.append(0)
                stmt_qlty.append("- Incorrect Date Format(0)")
            if sum(num_score) > 99:
                q_score.append(0.0)
                quality_string = "Missing + Incorrect Date Format > 99"
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


def cramers_corrected_stat(confusion_matrix):
    """ calculate Cramers V statistic for categorical-categorical association.
        uses correction from Bergsma and Wicher,
        Journal of the Korean Statistical Society 42 (2013): 323-328
    """
    chi2 = ss.chi2_contingency(confusion_matrix)[0]
    n = confusion_matrix.sum().sum()
    phi2 = chi2 / np.float(n)
    r, k = confusion_matrix.shape
    phi2corr = max(0, phi2 - ((k - 1) * (r - 1)) / (n - 1))
    rcorr = r - ((r - 1) ** 2) / (n - 1) * 1.0
    kcorr = k - ((k - 1) ** 2) / (n - 1) * 1.0
    return np.sqrt(phi2corr / min((kcorr - 1), (rcorr - 1)))


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


def identifyDaType(data_copy, Prob_type,target=None):
    # replace any space from feature names
    # data_copy.columns = data_copy.columns.str.replace(' ','')
    logger = utils.logger('Get','Info')
    # get headers of dataframe
    names = data_copy.columns.values.tolist()
    try:
        maxCat = int(utils.get_max_categories())
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
    
    if Prob_type == 'Clustering':
        variable="Clustering"
    elif Prob_type != "TimeSeries":
        uniquePercent = len(data_copy[target].unique()) * 1.0 / len(data_copy[target]) * 100
        if uniquePercent < 10:
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
def CheckCorrelation(data_copy, cols_to_remove, target):
    '''
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
                crossTabData = x.groupby([col1,col2])[col2].count().unstack().fillna(0)
                if len(list(crossTabData.shape)) > 1:
                    corrM[idx1, idx2] = cramers_corrected_stat(crossTabData)
                    corrM[idx2, idx1] = corrM[idx1, idx2]

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

    g_list = [target]
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
    '''
    corrDict={}
    corelated_Series=[]            
    return corrDict, pd.Series(corelated_Series)


def ChangeDataType(FilterData, columnsModified, columns_modifiedDict):
    text_cols = []
    id_cols = []
    unchangedColumns = {}
    maxCat = int(utils.get_max_categories())
    # set user assigned dataTypes
    for col in columnsModified:
        if columns_modifiedDict[col] == 'Text':
            text_cols.append(col)
            FilterData[col] = FilterData[col].astype(str)
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
                         msg = str(col)+": Cannot change "+x+" as datetime64[ns] datatype, as it contains alpha characters/numerical please validate"
                     elif x == 'float64' and y == 'int64':
                         msg = str(col)+": Cannot change float64 as int64 datatype ,as it contains  Null values please validate"
                     elif  x not in num_type and y in num_type:
                         msg = str(col)+": Cannot change "+x+" as "+y+" datatype ,as it contains  alpha characters/numerical  please validate"
                     else:
                         msg = str(col)+": Cannot change "+x+" as "+y+" datatype, please validate"
                     unchangedColumns[col] = msg
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

def CheckImbalance(data_copy, target, Skewed_cols):
    ImBalanced_col = []
    types_of_data = []
    ImbalanceDict = {}
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
            if len(class_Ratio == 2) and col == target:
                if max(class_Ratio) > 95:
                    ImBalanced = 3
                else:
                    ImBalanced = 0
            if len(class_Ratio == 2) and col != target:
                if max(class_Ratio) > 80:
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


def Data_Quality_Checks(data, target=None, Prob_type=None, DfModified=None, flag=None, text_cols=None, id_cols=None,
                        OrgnalTypes=None, scale_v=None):
    print("Flag is",flag)
    if flag == None:
        empty_cols = [col for col in data.columns if data[col].dropna().empty]
        data.drop(columns=empty_cols, inplace=True)
        OrgnalTypes = pd.Series([data[col].dtype.name for col in data.columns], index=data.columns).to_dict()
        data_copy, id_cols, date_cols, text_cols, incorrect_date = identifyDaType(data.copy(), target, Prob_type)
        if Prob_type == 'TimeSeries':
            OrgnalTypes[target] = data_copy[target].dtype.name
        cols_to_remove = id_cols + date_cols + text_cols
        
        if Prob_type=='Clustering':
            corrDict={} 
            corelated_Series=[]
        else:
            corrDict, corelated_Series = CheckCorrelation(data_copy, cols_to_remove, target)
        '''MAKE CHANGE, PASS PROBLEMTYPE'''
       

    elif flag == 2:
        id_cols = []
        text_cols = []
        date_cols = list(data.select_dtypes('datetime64[ns]').columns)
        OrgnalTypes = pd.Series([data[col].dtype.name for col in data.columns], index=data.columns).to_dict()
        incorrect_date = pd.Series(data=0, index=data.columns)
        corrDict, corelated_Series = CheckCorrelation(data.copy(), date_cols, target)
        data_copy = data.copy()


    else:
        date_cols = list(data.select_dtypes('datetime64[ns]').columns)
        incorrect_date = pd.Series(data=0, index=DfModified.columns)
        cols_to_remove = id_cols + date_cols + text_cols
        corrDict, corelated_Series = CheckCorrelation(data.copy(), cols_to_remove, target)
        temp = {}
        for col in DfModified.columns:
            temp[col] = OrgnalTypes[col]
        OrgnalTypes = temp
        data_copy = DfModified.copy()
        
    Skewed_cols = Check_Skew(data_copy)
    Outlier_Data_Count, Outlier_Data_Percent, percent_unique, \
    count_unique, percent_missing, count_missing, Outlier_Dict = CalculateStastics(data_copy)
    '''MAKE CHANGES FOR NOT INCLUDING IMBALANCE&QSCORE '''
    if Prob_type!='Clustering':
        ImBalanced_col, types_of_data, ImbalanceDict, Skewed = CheckImbalance(data_copy, target, Skewed_cols)
        Q_Scores, Q_Info = calculate_QScore(data_copy, percent_missing, Outlier_Data_Percent, Skewed, incorrect_date,
                                        ImBalanced_col)
    else:
        ImBalanced_col=pd.Series([])
        types_of_data=pd.Series([])
        ImbalanceDict={}
        Skewed={}

    columns = data_copy.columns
    # Recommendations = pd.Series(data = None,index = columns)
    ProblemType = pd.Series(data=0, index=columns)
    # Prescriptions = pd.Series(data = None,index = columns)
    # UnknownType = pd.Series(data = 0,index = columns)
    Data_quality_df = pd.DataFrame({'column_name': columns,
                                    'DataType': data_copy.dtypes,
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

    DtypeDict = {}
    ScaleDict = {}
    Balanced = []
    DateFormat = {}
    # Forming recommendation for each data quality check of columns
    for col in data_copy.columns:
        dtypelist2 = ["category", "float64","int64","datetime64[ns]","Id","Text"]

        O_N = ["Ordinal", "Nominal"]

        if data_copy[col].dtype.name == 'datetime64[ns]':
            DateFormat[col] = 'YYYY-MM-DD HH:MM:SS'
        if Data_quality_df.ImBalanced_col[col] == 0:
            Balanced.append("Yes")
        elif Data_quality_df.ImBalanced_col[col] == -1:
            Balanced.append("Not Applicable")
        else:
            Balanced.append("No")

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
            Data_quality_df.Data_Quality_Score[col] = 0
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

        #        data_copy[col].dtype.name  == 'category'
        if scale_v != None and data_copy[col].dtype.name == 'category':
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

    return Data_quality_df, Outlier_Dict, Skewed_cols, text_cols, target, \
           DtypeDict, ScaleDict, data_copy, corrDict, id_cols, corelated_Series, DateFormat


def checkDateNull(df):
    for col in df.columns:
        if df[col].dtype.name == "datetime64[ns]" and df[col].isnull().sum() > 0:
            df[col] = df[col].astype(str)
    return df


def UpdateData(correlationId, pageInfo, userId, flag):
    try:
        logger = utils.logger('Get', correlationId)

        utils.updQdb(correlationId, 'P', '20', pageInfo, userId)
        utils.logger(logger, correlationId, 'INFO', (
                    '\n' + 'data_quality_check' + '\'n' + "Updation initiated for flag= " + str(
                flag) + " correlation Id :" + str(correlationId) + "Data Quality Check"))

        typeDict, target, removedcols, UserInputColumns, dateFmts = utils.get_DataCleanUP_FilteredData(correlationId)
        data_t = utils.data_from_chunks(corid=correlationId, collection="PS_IngestedData")
        FilterData = data_t[UserInputColumns]

        FilterData.drop(columns=removedcols, inplace=True)

        [typeDict.pop(i, None) for i in removedcols]

        # typeDict = map(typeDict.pop,removedcols)
        FilterData = FilterData.astype(typeDict)

        df_cleanup, columns_modifiedDict, dtype, scale, scale_modified, OrginalTypes, targetType = utils.get_DataCleanUp(
            correlationId)
        df_cleanup = updateBinValues(df_cleanup)
        scale_v = {}
        for col in df_cleanup.index:
            for k, v in df_cleanup.Scale[col].items():
                if v == 'True':
                    scale_v[col] = k
        [OrginalTypes.pop(i, None) for i in removedcols]
        
        utils.logger(logger, correlationId, 'INFO',
                     ('\n' + 'data_quality_check' + '\'n' + "Fetched Data from Filtered data"))

        columnsModified = list(columns_modifiedDict.keys())
        unchangedColumns, FilterData, text_cols, id_cols = ChangeDataType(FilterData, columnsModified,
                                                                          columns_modifiedDict)

        if len(unchangedColumns.keys()) == len(columnsModified):

            utils.Update_DE_DataCleanup(correlationId, unchangedColumns, 1)
            utils.updQdb(correlationId, 'C', '100', pageInfo, userId)
            utils.logger(logger, correlationId, 'INFO',
                         ('\n' + 'data_quality_check' + '\'n' + "no changes are done for" + str(columnsModified) + ""))
        else:

            DfModified = FilterData[columnsModified]

            utils.updQdb(correlationId, 'P', '50', pageInfo, userId)
            # Data Clean up
            Quality_df, Outlier_Dict, Skewed_cols, text_cols, target, DtypeDict, ScaleDict, DfModified, \
            corrDict, id_cols, corelated_Series, DateFormat = Data_Quality_Checks(FilterData.copy(), target, None,
                                                                                  DfModified, flag, text_cols, id_cols,
                                                                                  OrginalTypes, scale_v)

            # unique values
            ColUniqueValues, typeDict = getUniqueValues(FilterData.copy())

            df_cleanup.drop(columns=['Correlation', 'Scale'], inplace=True)
            df_cleanup.columns = ['DataType', 'percent_unique', 'percent_missing', 'Outlier_Data', 'Balanced', \
                                  'IsSkewed', 'ImBalanced_col', 'OrdinalNominal', 'Binning_Values', \
                                  'Data_Quality_Score', 'ProblemType', 'Q_Info']
            #            O_N = ['Nominal', 'Ordinal']
            #            for k,v in scale_modified.items():
            #
            #                O_N.remove(v)
            #                O_N.insert(0,v)
            #                ScaleDict[k] = O_N
            # updating quality df and DtypeDict with new changes
            for col in Quality_df.index:
                dtype[col] = DtypeDict[col]
                scale[col] = ScaleDict[col]
                for param in df_cleanup.columns:
                    df_cleanup[param][col] = Quality_df[param][col]

            df_cleanup['column_name'] = list(df_cleanup.index)
            df_cleanup['CorelatedWith'] = corelated_Series

            UserInputColumns = list(set(UserInputColumns).difference(set(removedcols)))

            if len(DateFormat) > 0:
                DateFormat.update(dateFmts)
            else:
                DateFormat = dateFmts
            utils.Update_DataCleanUP_FilteredData(correlationId, ColUniqueValues, typeDict, UserInputColumns,
                                                  DateFormat, userId,'DataCleanUP_FilteredData')

            utils.logger(logger, correlationId, 'INFO',
                         ('\n' + 'data_quality_check' + '\'n' + "Data quality check executed"))
            utils.updQdb(correlationId, 'P', '70', pageInfo, userId)

            feature_name = utils.data_cleanup_json(df_cleanup, dtype, scale)

            utils.updQdb(correlationId, 'P', '90', pageInfo, userId)

            if int(targetType) != 4:
                TargetProblemType = df_cleanup.ProblemType[target]
            else:
                TargetProblemType = 4
            # utils.Update_DE_DataCleanup(feature_name,Outlier_Dict,corrDict,correlationId,str(TargetProblemType),unchangedColumns)
            flag = None
            utils.Update_DE_DataCleanup(correlationId, unchangedColumns, flag, feature_name, Outlier_Dict, corrDict,
                                        str(TargetProblemType), OrginalTypes, userId)
            utils.setRetrain(correlationId, True)

            utils.logger(logger, correlationId, 'INFO',
                         ('\n' + 'data_quality_check' + '\'n' + "Updated Data quality check results DE_DataCleanup"))

            utils.updQdb(correlationId, 'C', '100', pageInfo, userId)
    except Exception as e:
        utils.updQdb(correlationId, 'E', str(e.args), pageInfo, userId)
        utils.logger(logger, correlationId, 'ERROR', 'Trace')
        utils.save_Py_Logs(logger, correlationId)
    #        return jsonify({'status': 'false', 'message':str(e.args)}), 200
    else:
        utils.logger(logger, correlationId, 'INFO',
                     ('\n' + 'data_quality_check' + '\'n' + "Data quality check completed successfully"))
        utils.save_Py_Logs(logger, correlationId)


#        return jsonify({'status': 'true','message':"success"}), 200


def ViewDataQuality(correlationId, pageInfo, userId, flag):
    try:
        logger = utils.logger('Get', correlationId)

        utils.updQdb(correlationId, 'P', '20', pageInfo, userId)
        utils.logger(logger, correlationId, 'INFO', (
                    '\n' + 'data_quality_check' + '\'n' + "Updation initiated for flag= " + str(
                flag) + " correlation Id :" + str(correlationId) + "Data Quality Check"))

        # Fetch preprocessed data
        data = utils.data_from_chunks(correlationId, 'DE_PreProcessedData')
        '''CHANGES START HERE'''
        dbproconn, dbprocollection = utils.open_dbconn("ME_FeatureSelection")
        data_json = dbprocollection.find({"CorrelationId": correlationId})
        feature_not_created = data_json[0].get("Feature_Not_Created")
        features_created = data_json[0].get("Features_Created")
        encoded_new_feature = data_json[0].get("Map_Encode_New_Feature")

        '''CHANGES END HERE'''
        encoders = {}

        # Fetch data to be encoded from data processing table
        dbproconn, dbprocollection = utils.open_dbconn("DE_DataProcessing")
        data_json = dbprocollection.find({"CorrelationId": correlationId})
        dbproconn.close()

        Data_to_Encode = data_json[0].get('DataEncoding')
        '''CHANGES START HERE'''
        map_encode_new_feature = {}
        if len(encoded_new_feature) > 0:
            for i in range(len(encoded_new_feature)):
                map_encode_new_feature[encoded_new_feature[i]] = {'attribute': 'Nominal', 'encoding': 'Label Encoding',
                                                                  'ChangeRequest': 'True', 'PChangeRequest': 'False'}
        Data_to_Encode.update(map_encode_new_feature)

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
                encoders = {'OHE': {ohem: {'EncCols': enc_cols, 'OGCols': OHEcols}}}

            if len(LEcols) > 0:
                lencm, _, Lenc_cols, _ = utils.get_pickle_file(correlationId, FileType='LE')
                encoders.update({'LE': {lencm: Lenc_cols}})

            OGData = utils.get_OGDataFrame(data, encoders)
        else:
            OGData = data

        typeDict, target, removedcols, UserInputColumns, dateFmts = utils.get_DataCleanUP_FilteredData(correlationId)
        '''CHANGES START HERE'''
        if len(features_created) > 0:
            UserInputColumns = UserInputColumns + features_created
        '''CHANGES END HERE'''
        cols = list(typeDict.keys())
        '''CHANGES START HERE'''
        if len(features_created) > 0:
            cols = cols + features_created
        '''CHANGES END HERE'''

        for col in cols:
            if col not in OGData.columns:
                typeDict.pop(col, None)

        OGData = OGData.astype(typeDict)
        df_cleanup, dtype, scale = utils.get_DataCleanUpView(correlationId)
        df_cleanup = df_cleanup[df_cleanup.index.isin(OGData.columns)]
        utils.logger(logger, correlationId, 'INFO',
                     ('\n' + 'data_quality_check' + '\'n' + "Fetched Data from Filtered data"))

        Quality_df, Outlier_Dict, Skewed_cols, text_cols, target, DtypeDict, ScaleDict, DfModified, \
        corrDict, id_cols, corelated_Series, DateFormat = Data_Quality_Checks(OGData, target, None, None, flag)

        # unique values
        #            ColUniqueValues,typeDict = getUniqueValues(FilterData.copy())

        df_cleanup.drop(columns=['Correlation', 'Scale'], inplace=True)
        df_cleanup.columns = ['DataType', 'percent_unique', 'percent_missing', 'Outlier_Data', 'Balanced', \
                              'IsSkewed', 'ImBalanced_col', 'OrdinalNominal', 'Binning_Values', \
                              'Data_Quality_Score', 'ProblemType', 'Q_Info']

        '''CHANGES START HERE'''
        if len(features_created) > 0:
            for item in features_created:
                data = Quality_df.loc[item, df_cleanup.columns]
                df_cleanup = df_cleanup.append(data)
        '''CHANGES END HERE'''
        # updating quality df and DtypeDict with new changes
        for col in Quality_df.index:
            for param in df_cleanup.columns:
                df_cleanup[param][col] = Quality_df[param][col]

        df_cleanup['column_name'] = list(df_cleanup.index)
        df_cleanup['CorelatedWith'] = corelated_Series

        utils.logger(logger, correlationId, 'INFO',
                     ('\n' + 'data_quality_check' + '\'n' + "Data quality check executed"))
        utils.updQdb(correlationId, 'P', '70', pageInfo, userId)

        feature_name = utils.data_cleanup_json(df_cleanup, dtype, scale)
        utils.updQdb(correlationId, 'P', '90', pageInfo, userId)
        dbconn, dbcollection = utils.open_dbconn('DE_DataCleanup')
        dbcollection.update_many({"CorrelationId": correlationId}, {'$set': {"ViewDataQuality": feature_name}})
        dbconn.close()

        utils.logger(logger, correlationId, 'INFO',
                     ('\n' + 'data_quality_check' + '\'n' + "Updated Data quality check results DE_DataCleanup"))

        utils.updQdb(correlationId, 'C', '100', pageInfo, userId)
    except Exception as e:
        utils.updQdb(correlationId, 'E', str(e.args), pageInfo, userId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID)
        utils.logger(logger, correlationId, 'ERROR', 'Trace')
        utils.save_Py_Logs(logger, correlationId)
    #        return jsonify({'status': 'false', 'message':str(e.args)}), 200
    else:
        utils.logger(logger, correlationId, 'INFO',
                     ('\n' + 'data_quality_check' + '\'n' + "Data quality check completed successfully"))
        utils.save_Py_Logs(logger, correlationId)


def main(correlationId, pageInfo, userId,UniId=None,ClientID=None,DCUID=None,ServiceID=None,userdefinedmodelname=None,EnDeRequired=None):
   try:
        logger = utils.logger('Get', correlationId)
        if EnDeRequired == None or not isinstance(EnDeRequired,bool):
            raise Exception("Encryption Flag is a mandatory field")
        
        utils.updQdb(correlationId, 'P', '10', pageInfo, userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
        utils.logger(logger, correlationId, 'INFO', (
                    '\n' + 'data_quality_check' + '\'n' + "Process initiated for " + str(
                pageInfo) + " correlation Id :" + str(correlationId) + "Data Quality Check"))
        try:
            dbconn, dbcollection = utils.open_dbconn("PS_BusinessProblem")
            data_json = list(dbcollection.find({"CorrelationId": correlationId}))
            dbconn.close()
            if len(data_json)!=0:
                target_variable = data_json[0]['TargetColumn']
                UniqueIdentifir = data_json[0]['TargetUniqueIdentifier']
                if data_json[0]['ProblemType'] != "TimeSeries":
                    UserInputColumns = data_json[0]['InputColumns']
                    Prob_type = None
                else:
                    UserInputColumns = [data_json[0]['TimeSeries']['TimeSeriesColumn']]
                    Prob_type = data_json[0]['ProblemType']
            else:
                 dbconn, dbcollection = utils.open_dbconn("Clustering_IngestData")
                 data_json = list(dbcollection.find({"CorrelationId": correlationId}))
                 dbconn.close()
                 #Prob_type = list(data_json[0].get('ProblemType').keys())[0]
                 Prob_type='Clustering'
                 print('Step1')
        except Exception as e:
             utils.updQdb(correlationId, 'E', e.args[0], pageInfo, userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
             utils.logger(logger, correlationId, 'ERROR', 'Trace')
            

#        utils.logger(logger, correlationId, 'INFO', (
#                    '\n' + 'data_quality_check' + '\'n' + "page info:  " + str(pageInfo) + " Target :" + str(
#                target_variable) + '\n' + "Input Columns :" + str(UserInputColumns) + '\n' + "from PS_BusinessProblem"))

        # Adding target column filter dafta for all column from base data
        try:
            UserInputColumns.insert(0, target_variable)
            if UniqueIdentifir != None:
                UserInputColumns.insert(0, UniqueIdentifir)
        except:
            utils.logger(logger, correlationId, 'INFO',"update UniqueIdentifir")
        

        # Fetch data
        utils.updQdb(correlationId, 'P', '20', pageInfo, userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
        offlineutility = utils.checkofflineutility(correlationId)
        if offlineutility:
            data_t = utils.data_from_chunks_offline_utility(corid=correlationId, collection="DataSet_IngestData",lime=None, recent=None,DataSetUId=offlineutility)
        else:
            data_t = utils.data_from_chunks(corid=correlationId,collection="Clustering_BusinessProblem")  

        print('Step2')

        utils.logger(logger, correlationId, 'INFO',
                     ('\n' + 'data_quality_check' + '\'n' + "Fetched Data from PS_IngestedData"))

        # Data Clean up
        utils.updQdb(correlationId, 'P', '50', pageInfo, userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
        
        # Data_quality_df,Outlier_Dict,Skewed_cols,text_cols,target,DtypeDict,ScaleDict,data_copy,corrDict = Data_Quality_Checks(ingedata.copy(),target)
        print("The problemtype is: ",Prob_type)
        Data_quality_df, Outlier_Dict, Skewed_cols, text_cols, target, DtypeDict, \
        ScaleDict, data_copy, corrDict, id_cols, corelated_Series, DateFormat = Data_Quality_Checks(
            data_t, Prob_type)
        Data_quality_df['CorelatedWith'] = pd.Series(data=None,index=Data_quality_df.index)
        ColUniqueValues, typeDict = getUniqueValues(data_copy)
        print('Step3')


        utils.updQdb(correlationId, 'P', '70', pageInfo, userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
        #        data_copy = checkDateNull(data_copy)
        OrgDtypes = Data_quality_df.originalDtyps.to_dict()

        if Prob_type != "TimeSeries" and Prob_type != "Regression" and Prob_type!='Clustering':
            TargetProblemType = Data_quality_df.ProblemType[target]
        elif Prob_type == "Regression":
            TargetProblemType = 1
        elif Prob_type == "TimeSeries":
            TargetProblemType = 4
        else:
            #            if OrgDtypes[target] in num_type:
            #                 Data_quality_df.DataType[target] = 'float64'
            #                 typeDict[target] = 'float64'
            #                 DtypeDict[target] = [ 'float64', 'category','int64', 'Id']
            TargetProblemType = 5
        
        for key,value in ColUniqueValues.items():
            if isinstance(value,list):
                ColUniqueValues[key] = [str(i) for i in value]
                
        if EnDeRequired:
            ColUniqueValues = EncryptData.EncryptIt(json.dumps(ColUniqueValues))
        
        if TargetProblemType!=5:
            utils.save_DataCleanUP_FilteredData(correlationId, ColUniqueValues, typeDict, UserInputColumns,
                                            DateFormat, userId,target_variable,'DataCleanUP_FilteredData')
        else:
             UserInputColumns=[]
             utils.save_DataCleanUP_FilteredData(correlationId, ColUniqueValues, typeDict, UserInputColumns,
                                            DateFormat, userId,collection='Clustering_DataCleanUP_FilteredData')

        utils.logger(logger, correlationId, 'INFO',
                     ('\n' + 'data_quality_check' + '\'n' + "Data quality check executed"))

        utils.updQdb(correlationId, 'P', '90', pageInfo, userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)


        feature_name = utils.data_cleanup_json(Data_quality_df, DtypeDict, ScaleDict)

        if EnDeRequired:
            feature_name = EncryptData.EncryptIt(json.dumps(feature_name,default=json_util.default))
        
        utils.save_DE_DataCleanup(feature_name, Outlier_Dict, corrDict, correlationId, str(TargetProblemType),
                                  OrgDtypes, userId,'Clustering_DE_DataCleanup')

        utils.logger(logger, correlationId, 'INFO',
                     ('\n' + 'data_quality_check' + '\'n' + "Saved Data quality check results DE_DataCleanup"))

        utils.updQdb(correlationId, 'C', '100', pageInfo, userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
   except Exception as e:
        utils.updQdb(correlationId, 'E', e.args[0], pageInfo, userId,UniId=UniId,ClientID=ClientID,DCUID=DCUID,ServiceID=ServiceID,userdefinedmodelname=userdefinedmodelname)
        utils.logger(logger, correlationId, 'ERROR', 'Trace')
        utils.save_Py_Logs(logger, correlationId)
        raise Exception (e.args[0]) 
    #        return jsonify({'status': 'false', 'message':str(e.args)}), 200
   else:
        utils.logger(logger, correlationId, 'INFO',
                     ('\n' + 'data_quality_check' + '\'n' + "Data quality check completed successfully"))
        utils.save_Py_Logs(logger, correlationId)
#        return jsonify({'status': 'true','message':"success"}), 200








