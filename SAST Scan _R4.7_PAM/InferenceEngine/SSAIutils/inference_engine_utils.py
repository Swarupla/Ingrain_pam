# -*- coding: utf-8 -*-
"""
Created on Thu Feb 25 10:56:46 2021

@author: maddikuntla.p.kumar
"""
import warnings
warnings.filterwarnings("ignore")
import pandas as pd
import numpy as np
#from pandas import *
# from libraries.settings import *
from scipy.stats.stats import pearsonr
import itertools
import statsmodels.api as sm 
from statsmodels.formula.api import ols
import xlrd
from scipy import stats 
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import LabelEncoder
from math import sqrt
from sklearn.metrics import mean_squared_error
#from  matplotlib import pyplot 
from sklearn.linear_model import LinearRegression
import calendar
from sklearn.ensemble import RandomForestClassifier
from statsmodels.tsa.arima.model import ARIMA
import dateutil.parser
from datetime import datetime
import inflect
import json
import random


import math
import re 


# def get_uniqueValues(df1):
#     df=df1.copy()
#     #droping the columns which has all null values
#     null_value_columns=df.columns[df.isnull().sum()>=0.9*df.shape[0]].tolist()
#     df.drop(null_value_columns,axis=1,inplace=True)
#     #df.dropna(inplace=True)
#     #df.to_excel('df.xlsx')
#     #get counters of each value in each dimension
#     uniqueValues_dict = dict(zip([i for i in df.columns] , [pd.DataFrame(df[i].dropna().unique(), columns=[i]) for i in df.columns]))
#     return df,uniqueValues_dict,null_value_columns

def get_uniqueValues(df1):
    df=df1.copy()
    #droping the columns which has all null values
    null_value_columns=df.columns[df.isnull().sum()>=0.9*df.shape[0]].tolist()
    df.drop(null_value_columns,axis=1,inplace=True)
    #converting all boolean values into string
    boolcols=df.select_dtypes(include=['bool']).columns.tolist()
    for col in boolcols:
        df[col]=df[col].astype(str)
    #get counters of each value in each dimension
    uniqueValues_dict = dict(zip([i for i in df.columns] , [list(df[i].dropna().unique()) for i in df.columns]))
    return df,uniqueValues_dict,null_value_columns


def identify_date_columns(df1):
#Identify Date columns which needs to be removed as we are expecting this from the user
    df=df1.copy()
    datecolumns = []
    #df=df.dropna()

    for col in df.columns:
        df_col = df[col].dropna()
        if str(df_col.dtype) in ['datetime64[ns]', 'datetime64[ns, UTC]','datetime64[ns, tzlocal()]'] :
             datecolumns.append(col)
     

    
    return datecolumns
        
"""
data_df=databins
Values_dict=updatedUniquedict
datecolumns=date_columns
null_value_columns
bincolumns
"""

# def get_important_columns(data_df,Values_dict,datecolumns,null_value_columns,bincolumns):
#     impcolumns_dict = {}
#     suggested_impcolumns_dict={}
#     targetcolumns=[]
#     singleValueColumns = []
#     removed_columns={}
#     filtervalues={}
#     cutoff_2per=int(data_df.shape[0]*0.02)
#     cutoff_50per=int(data_df.shape[0]*0.5)
    
#     #get all numerical columns)
#     numerics_columns = data_df.select_dtypes(include=['int64','float64','int32','float32']).columns.tolist()

    
#     #Identify columns which has ONLY 1 value for the entire data set .. these type of columns doesnt give us any information
#     for key, value in Values_dict.items():
#         if(len(value) == 1):
#             singleValueColumns.append(key)

#     #Identify Important columns = all columns - (numerical columns+datatype columns + single value columns + columns where values is more than 50%)
#     for key, value in Values_dict.items():
#         if(len(value) <= cutoff_50per and key not in numerics_columns and key not in datecolumns and key not in singleValueColumns or key in bincolumns):
#             impcolumns_dict[key]=len(value)
#             filtervalues[key]=value
        
#         if(len(value) > cutoff_50per and key not in numerics_columns and key not in datecolumns and key not in bincolumns):
#            suggested_impcolumns_dict[key]=len(value)
         
         
#         #removed columna    
#         if(len(value)) >cutoff_50per:
#             removed_columns[key]='Unique Values are more than 50% of whole data'
#         if key in numerics_columns:
#             removed_columns[key]='Continous Column'
#         if key in datecolumns:
#             removed_columns[key]='Date Column'
#         if key in singleValueColumns:
#             removed_columns[key]='SingleValue Column'
#     for key in null_value_columns:
#         removed_columns[key]='NullValue Column'
        
#     #sorting the dict  inorder to get the keys list whose  len(value) are less at First
#     impcolumns =sorted(impcolumns_dict, key=impcolumns_dict.get)
#     suggested_impcolumns =sorted(suggested_impcolumns_dict, key=suggested_impcolumns_dict.get)
#     sorted_filterdict={}
#     for w in impcolumns:
#         sorted_filterdict[w] = filtervalues[w]
        
#     targetcolumns=list(set(impcolumns+numerics_columns))
    
#     return impcolumns,targetcolumns,removed_columns,suggested_impcolumns,sorted_filterdict

def get_important_columns(data_df,Values_dict,datecolumns,null_value_columns,bincolumns):
    impcolumns_dict = {}
    suggested_impcolumns_dict={}
    targetcolumns=[]
    singleValueColumns = []
    remove_numericalcols=[]

    removed_columns={}
    filtervalues={}
    cutoff_2per=int(data_df.shape[0]*0.02)
    cutoff_50per=int(data_df.shape[0]*0.5)
    
    #get all numerical columns)
    numerics_columns = data_df.select_dtypes(include=['int64','float64','int32','float32']).columns.tolist()

    
    #Identify columns which has ONLY 1 value for the entire data set .. these type of columns doesnt give us any information
    for key, value in Values_dict.items():
        if(len(value) == 1):
            singleValueColumns.append(key)
            
    #Identify Important columns = all columns - (numerical columns+datatype columns + single value columns + columns where values is more than 50%)
    for key, value in Values_dict.items():
        if(len(value) <= cutoff_50per and key not in numerics_columns and key not in datecolumns and key not in singleValueColumns or key in bincolumns):
            impcolumns_dict[key]=len(value)
            filtervalues[key]=value
        
        if(len(value) > cutoff_50per and key not in numerics_columns and key not in datecolumns and key not in bincolumns):
           suggested_impcolumns_dict[key]=len(value)
        
        if key.endswith("-categorized"):
            remove_numericalcols.append(key.split("-categorized")[0])

        #removed columna    
        if(len(value)) >cutoff_50per:
            removed_columns[key]='Unique Values are more than 50% of whole data'
        if key in numerics_columns:
            removed_columns[key]='Continous Column'
        if key in datecolumns:
            removed_columns[key]='Date Column'
        if key in singleValueColumns:
            removed_columns[key]='SingleValue Column'
    for key in null_value_columns:
        removed_columns[key]='NullValue Column'
    #sorting the dict  inorder to get the keys list whose  len(value) are less at First
    impcolumns =sorted(impcolumns_dict, key=impcolumns_dict.get)
    suggested_impcolumns =sorted(suggested_impcolumns_dict, key=suggested_impcolumns_dict.get)
    sorted_filterdict={}
    for w in impcolumns:
        sorted_filterdict[w] = filtervalues[w]
        
    targetcolumns=list(set(impcolumns+numerics_columns)-set(remove_numericalcols))
    
    return impcolumns,targetcolumns,removed_columns,suggested_impcolumns,sorted_filterdict

        

#date_by_user='Reported Date'

def add_datecolumn_by_user(df,impcolumns,date_by_user):
    # impcolumns.append(date_by_user)
    df_copy=df.copy()
    #df_copy[date_by_user] = datetime.strptime(str(df_copy[date_by_user]), '%Y-%m-%d %H:%M:%S,%f')

    #break  date by user into time elements like year, month, day and Quarter then drop the actual column which is of no more use.
    df_copy[date_by_user+'_Year'] = pd.DatetimeIndex(df[date_by_user],dayfirst=True).year
    df_copy[date_by_user+'_Month'] = pd.DatetimeIndex(df[date_by_user],dayfirst=True).month
    df_copy[date_by_user+'_Day'] =pd.DatetimeIndex( df[date_by_user],dayfirst=True).day
    df_copy[date_by_user+'_Quarter'] =pd.DatetimeIndex(df[date_by_user],dayfirst=True).quarter
    df_copy[date_by_user+'_Hour'] =pd.DatetimeIndex(df[date_by_user],dayfirst=True).hour
    df_copy[date_by_user+'_Weekday'] =pd.DatetimeIndex(df[date_by_user],dayfirst=True).weekday
    #df_copy[date_by_user+'_DateStamp']=pd.DatetimeIndex(df[date_by_user],dayfirst=True).date
    
    df_fortrend=df_copy[date_by_user]
    #removing date_by_user
    df_copy.drop([date_by_user], axis = 1,inplace=True)
    
    # impcolumns.remove(date_by_user)
    impcolumns.append(date_by_user+'_Year')
    impcolumns.append(date_by_user+'_Month')
    impcolumns.append(date_by_user+'_Day')
    impcolumns.append(date_by_user+'_Quarter')
    impcolumns.append(date_by_user+'_Hour')
    impcolumns.append(date_by_user+'_Weekday')
    #impcolumns.append(date_by_user+'_DateStamp')


    
    return df_copy,df_fortrend,impcolumns


def addingDateColumnInMetric(df,impcolumns,date_by_user):
    df_copy=df.copy()

    #break  date by user into time elements like year, month, day and Quarter then drop the actual column which is of no more use.
    df_copy[date_by_user+'_Year'] = pd.DatetimeIndex(df[date_by_user],dayfirst=True).year
    df_copy[date_by_user+'_Month'] = pd.DatetimeIndex(df[date_by_user],dayfirst=True).month
    df_copy[date_by_user+'_Day'] =pd.DatetimeIndex( df[date_by_user],dayfirst=True).day
    # df_copy[date_by_user+'_Quarter'] =pd.DatetimeIndex(df[date_by_user],dayfirst=True).quarter
    df_copy[date_by_user+'_Hour'] =pd.DatetimeIndex(df[date_by_user],dayfirst=True).hour
    df_copy[date_by_user+'_Weekday'] =pd.DatetimeIndex(df[date_by_user],dayfirst=True).weekday
    #df_copy[date_by_user+'_DateStamp']=pd.DatetimeIndex(df[date_by_user],dayfirst=True).date
    
    df_fortrend=df_copy[date_by_user]
    #removing date_by_user
    df_copy.drop([date_by_user], axis = 1,inplace=True)
    
    impcolumns.append(date_by_user+'_Year')
    impcolumns.append(date_by_user+'_Month')
    impcolumns.append(date_by_user+'_Day')
    # impcolumns.append(date_by_user+'_Quarter')
    impcolumns.append(date_by_user+'_Hour')
    impcolumns.append(date_by_user+'_Weekday')


    
    return df_copy,impcolumns

"""binning function"""
"""
df=data_copy.copy()
values_dict=unique_values_dict.copy()
threshold
"""
# def binningContinousColumn(df,values_dict,threshold):
#     bincolumns=[]
#     data_with_bins=df.copy()
#     #_,unique_values_dict,_=inference_engine_utils.get_uniqueValues(data_with_bins)
#     numerics_columns = data_with_bins.select_dtypes(include=['int64','float64','int32','float32']).columns.tolist()
        
#     for num_col in numerics_columns:
#         unique_val=len(values_dict[num_col])
        
#         if unique_val <=threshold :
#             data_with_bins[num_col]=data_with_bins[num_col].fillna('Null') #replacing null values with mode            
#             data_with_bins[num_col]=data_with_bins[num_col].astype(str)
#             binlist=data_with_bins[num_col].value_counts().index.tolist()
#             values_dict[num_col]=binlist
#             bincolumns.append(num_col)
#         else:
#             try:
#                 roundedcol=np.round(data_with_bins[num_col])
#             except ValueError:
#                 pass
#             nunique=roundedcol.nunique()
#             noofbins=round(nunique*0.25)
#             """
#             limiting the no of bins to threshold value because some times bin values are higher in number
#             """
#             if noofbins >threshold and threshold >=2:  
#                 noofbins=threshold
#             elif noofbins ==0 or noofbins ==1:
#                 noofbins=round(nunique)          

#             min_val=roundedcol.min()
#             max_val=roundedcol.max()
#             width=(max_val-min_val)/noofbins
#             if width >5: #when rounding  <=5 it becomes zero
#                 width=10*round(width/10)
#             else:
#                 width=round(width)
#                 if width <=0:
#                     width=0.5
                
            
#             bins = np.arange(0, max_val+width, width,dtype='int32')   #here end point is taken as max+width so that last value  will come as greater than max value
#             interval=pd.IntervalIndex.from_breaks(bins)
#             intervaltemp1=pd.Series(interval).apply(lambda x: pd.Interval(left=0,right=int(x.right),closed='both') if x.left <= 0 else pd.Interval(left=int(x.left),right=int(x.right),closed='right'))
#             temp=pd.cut(roundedcol,bins=bins,include_lowest=True,precision=0)
#             temp1=temp.apply(lambda x: pd.Interval(left=0,right=int(x.right),closed='both') if x.left <= 0 else pd.Interval(left=int(x.left),right=int(x.right),closed='right'))
            
#             data_with_bins[num_col+"-bin"]=temp1.astype(str)
#             data_with_bins[num_col+"-bin"]=data_with_bins[num_col+"-bin"].replace(to_replace={'nan':'Null','NaN':'Null'})
            
#             leftinterval= pd.Interval(left=float('-inf'),right=0,closed='neither') #left interval  (-inf,0)
    
#             finalbins=[str(leftinterval)]
#             finalbins=finalbins+list(intervaltemp1.astype(str))
#             rightinterval=pd.Interval(left=bins.max(),right=float('inf'),closed='neither')
#             finalbins.append(str(rightinterval))
#             # binsforcol=temp1.unique().tolist()
#             # if np.nan in binsforcol:
#             #     finalbins.append('Null')
#             finalbins.append("Null")
#             values_dict[num_col+"-bin"]=finalbins
#             bincolumns.append(num_col+"-bin")
             
#     return data_with_bins,values_dict,bincolumns,numerics_columns

def binningContinousColumn(df,values_dict,threshold):
    bincolumns=[]
    data_with_bins=df.copy()
    #_,unique_values_dict,_=inference_engine_utils.get_uniqueValues(data_with_bins)
    numerics_columns = data_with_bins.select_dtypes(include=['int64','float64','int32','float32']).columns.tolist()
        
    for num_col in numerics_columns:
        unique_val=len(values_dict[num_col])

        if unique_val <=threshold :
            data_with_bins[num_col+"-categorized"]=data_with_bins[num_col].fillna('Null') #replacing null values with mode            
            data_with_bins[num_col+"-categorized"]=data_with_bins[num_col+"-categorized"].astype(str)
            binlist=data_with_bins[num_col+"-categorized"].value_counts().index.tolist()
            values_dict[num_col+"-categorized"]=binlist
            bincolumns.append(num_col+"-categorized")
        else:
            try:
                #print(num_col)
                try:
                    roundedcol=np.round(data_with_bins[num_col])
                except ValueError as e:
                    error_value=e.args
                nunique=roundedcol.nunique()
                noofbins=round(nunique*0.25)
                #print(noofbins)
                """
                limiting the no of bins to threshold value because some times bin values are higher in number
                """
                if noofbins >threshold and threshold >=2:  
                    #print(noofbins,":::",threshold)
                    noofbins=threshold
                elif noofbins ==0 or noofbins ==1:
                    noofbins=nunique          
    
                min_val=roundedcol.min()
                max_val=roundedcol.max()
                width=(max_val-min_val)/noofbins
                #print("Before width",width,"max_val",max_val)
                if width >5: #when rounding  <=5 it becomes zero
                    width=10*round(width/10)
                else:
                    width=round(width)
                    if width <=0:
                        width=0.5
                #print("After width:",width)
                
                """ changes start"""
                if width >0.5:
                    try:
                        #print("In try block")
                        bins = np.arange(0, max_val+width, width,dtype='int32')   #here end point is taken as max+width so that last value  will come as greater than max value
                        interval=pd.IntervalIndex.from_breaks(bins)
                    except: 
                        bins = np.arange(0, max_val+width, width) #try except is added  "data specific issue in PAM Minspec entity flow"
                        interval=pd.IntervalIndex.from_breaks(bins)
                        
                    intervaltemp1=pd.Series(interval).apply(lambda x: pd.Interval(left=0,right=int(x.right),closed='both') if x.left <= 0 else pd.Interval(left=int(x.left),right=int(x.right),closed='right'))
                    temp=pd.cut(roundedcol,bins=bins,include_lowest=True,precision=0)
                    temp1=temp.apply(lambda x: pd.Interval(left=0,right=int(x.right),closed='both') if x.left <= 0 else pd.Interval(left=int(x.left),right=int(x.right),closed='right'))
                else:
                    bins = np.arange(0, max_val+width, width)
                    #print("bins in else:::"+str(bins))
                    interval=pd.IntervalIndex.from_breaks(bins)
                    intervaltemp1=pd.Series(interval).apply(lambda x: pd.Interval(left=0,right=x.right,closed='both') if x.left <= 0 else pd.Interval(left=x.left,right=x.right,closed='right'))
                    temp=pd.cut(roundedcol,bins=bins,include_lowest=True,precision=0)
                    #print(temp)
                    temp1=temp.apply(lambda x: pd.Interval(left=0,right=x.right,closed='both') if x.left <= 0 else pd.Interval(left=x.left,right=x.right,closed='right'))
                
                    """ changes end"""
                
                data_with_bins[num_col+"-bin"]=temp1.astype(str)
                data_with_bins[num_col+"-bin"]=data_with_bins[num_col+"-bin"].replace(to_replace={'nan':'Null','NaN':'Null'})
                
                leftinterval= pd.Interval(left=float('-inf'),right=0,closed='neither') #left interval  (-inf,0)
        
                finalbins=[str(leftinterval)]
                finalbins=finalbins+list(intervaltemp1.astype(str))
                rightinterval=pd.Interval(left=bins.max(),right=float('inf'),closed='neither')
                finalbins.append(str(rightinterval))
                # binsforcol=temp1.unique().tolist()
                # if np.nan in binsforcol:
                #     finalbins.append('Null')
                finalbins.append("Null")
                values_dict[num_col+"-bin"]=finalbins
                bincolumns.append(num_col+"-bin")
            except :  #exception is handled for Memory issue occuring when bins are creating for larger numerical values(>10 digits) such as 454594859, etc, it is mainly data specific issue
                data_with_bins[num_col+"-categorized"]=data_with_bins[num_col].fillna('Null') #replacing null values with mode            
                data_with_bins[num_col+"-categorized"]=data_with_bins[num_col+"-categorized"].astype(str)
                binlist=data_with_bins[num_col+"-categorized"].value_counts().index.tolist()
                values_dict[num_col+"-categorized"]=binlist
                bincolumns.append(num_col+"-categorized")
                 
    return data_with_bins,values_dict,bincolumns,numerics_columns


""" end for binning"""
def rangetostr(ran):
    if ran !='Null':
        ran=ran.replace(ran[0],"").replace(ran[-1],"")
        range1,range2=ran.split(', ')
          
        return str(range1)+" - "+str(range2)
    else:
        return 'Null'
            

    
def formatBinValues(databins,bincol):
    dfbin=databins.copy()
    for col in bincol:
        if col.endswith('-bin'):
            dfbin[col]=dfbin[col].apply(lambda c:rangetostr(c))
    return dfbin


def assignFalseForFilterValues(f_dict):
    filter_dict={}
    for key ,values in f_dict.items():
        temp_dict={}
        for val in  values:
            temp_dict[str(val)]='False'
        filter_dict[key]=temp_dict
    
    filterjsonstr=json.dumps(filter_dict,sort_keys=True)
    return filterjsonstr


# def filterInMeasureAnalysis(dff,filters,num_cols):
#     df_filter=dff.copy()

#     for key,value in filters.items():
        
#         #if value.get('ChangeRequest') == 'True' and value.get('PChangeRequest') != 'True':
#         values_t = [key1 for key1,value1 in value.items() if value1=='True'and (key1 != 'ChangeRequest' and key1!='PChangeRequest')]             
#         if len(values_t): 
#             if key.split("-bin")[0] not in num_cols:
#                 df_filter= df_filter.loc[df_filter[key].isin(values_t)]
#                 #value['PChangeRequest'] = "True"
#             else:
#                 if key.endswith('-bin'):
#                     col=key.split("-bin")[0]
#                     num_col_df=pd.DataFrame(columns=df_filter.columns)
#                     for val in values_t:
#                         if val !='Null':
#                             op1=val[0]
#                             op2=val[-1]
#                             val=val.replace(val[0],"").replace(val[-1],"")
#                             range1,range2=val.split(', ')
#                             num1=float(range1)
#                             num2=float(range2)
                            
                            
#                             if op1 =='(' and op2 ==')':
#                                 temp=df_filter.loc[(df_filter[col] > num1) & (df_filter[col] < num2) ]
#                             elif op1 =='[' and op2 ==')':
#                                 temp=df_filter.loc[(df_filter[col] >= num1) & (df_filter[col] < num2 )]

#                             elif op1 =='(' and op2 ==']':
#                                 temp=df_filter.loc[(df_filter[col] > num1) & (df_filter[col] <= num2) ]

                            
#                             elif op1 =='[' and op2 ==']': 
#                                 temp=df_filter.loc[(df_filter[col] >= num1) & (df_filter[col] <= num2) ]
                            
#                         else:
#                             temp=df_filter.loc[(df_filter[col] == 'Null') | (df_filter[col] == np.nan) ]
#                         num_col_df=pd.concat([num_col_df,temp])
                        
                    
#                     df_filter=num_col_df.copy()
#                     #value['PChangeRequest'] = "True"

                    
#                 else:
#                     df_filter= df_filter.loc[df_filter[key].isin(values_t)]
#                     #value['PChangeRequest'] = "True"

#     return df_filter

def filterInMeasureAnalysis(dff,filters,num_cols):
    df_filter=dff.copy()

    for key,value in filters.items():
        
        values_t = [key1 for key1,value1 in value.items() if value1=='True']             
        if len(values_t): 
            
            if key.endswith('-bin'):
                col=key.split("-bin")[0]
                num_col_df=pd.DataFrame(columns=df_filter.columns)
                for val in values_t:
                    if val !='Null':
                        op1=val[0]
                        op2=val[-1]
                        val=val.replace(val[0],"").replace(val[-1],"")
                        range1,range2=val.split(', ')
                        num1=float(range1)
                        num2=float(range2)
                        
                        
                        if op1 =='(' and op2 ==')':
                            temp=df_filter.loc[(df_filter[col] > num1) & (df_filter[col] < num2) ]
                        elif op1 =='[' and op2 ==')':
                            temp=df_filter.loc[(df_filter[col] >= num1) & (df_filter[col] < num2 )]

                        elif op1 =='(' and op2 ==']':
                            temp=df_filter.loc[(df_filter[col] > num1) & (df_filter[col] <= num2) ]

                        
                        elif op1 =='[' and op2 ==']': 
                            temp=df_filter.loc[(df_filter[col] >= num1) & (df_filter[col] <= num2) ]
                        
                    else:
                        temp=df_filter.loc[(df_filter[col] == 'Null') | (df_filter[col] == np.nan) ]
                    num_col_df=pd.concat([num_col_df,temp])
                    
                
                df_filter=num_col_df.copy()
                
                
            elif key.endswith("-categorized") :
                colval=key.split("-categorized")[0]
                temp_values=[]
                for val in values_t:
                    if val!='Null':
                        try:
                            temp_values.append(eval(val))
                        except Exception as e:
                            error_encounterd = str(e.args[0])
                    else:
                        temp_values.append(np.nan)
                        
                    
                df_filter= df_filter.loc[df_filter[colval].isin(temp_values)]
            else:
                df_filter= df_filter.loc[df_filter[key].isin(values_t)]

                

    return df_filter





def encode_date_columns_volumetricAnalysis(df,year,month,day,quarter,weekday,hour):
    selected_data_counter_EncodeDateparts=df.copy()
    selected_data_counter_EncodeDateparts[year]=selected_data_counter_EncodeDateparts[year].astype(int).astype(str)
    selected_data_counter_EncodeDateparts[day]=selected_data_counter_EncodeDateparts[day].astype(int).astype(str)
    selected_data_counter_EncodeDateparts[hour]=selected_data_counter_EncodeDateparts[hour].astype(int).astype(str)
    selected_data_counter_EncodeDateparts[month].replace({
        1:'January',2:'February',3:'March',4:'April',5:'May',6:'June',7:'July',
        8:'August',9:'September',10:'October',11:'November',12:'December'
        },inplace=True)
    selected_data_counter_EncodeDateparts[weekday].replace({
        0:'Monday',1:'Tuesday',2:'Wednesday',
        3:'Thurday',4:'Friday',5:'Saturday',6:'Sunday'},inplace=True)
    selected_data_counter_EncodeDateparts[quarter].replace({
        1: 'Quarter-1',2: 'Quarter-2', 3: 'Quarter-3', 4: 'Quarter-4'
        },inplace=True)    
    
    return selected_data_counter_EncodeDateparts

    
def encode_date_columns(df,year,month,day,weekday,hour):
    selected_data_counter_EncodeDateparts=df.copy()
    selected_data_counter_EncodeDateparts[year]=selected_data_counter_EncodeDateparts[year].astype("Int64")
    selected_data_counter_EncodeDateparts[year]=selected_data_counter_EncodeDateparts[year].replace(np.nan,"Null").astype(str)
    selected_data_counter_EncodeDateparts[day]=selected_data_counter_EncodeDateparts[day].astype("Int64")
    selected_data_counter_EncodeDateparts[day]=selected_data_counter_EncodeDateparts[day].replace(np.nan,"Null").astype(str)
    selected_data_counter_EncodeDateparts[hour]=selected_data_counter_EncodeDateparts[hour].astype("Int64")
    selected_data_counter_EncodeDateparts[hour]=selected_data_counter_EncodeDateparts[hour].replace(np.nan,"Null").astype(str)

    selected_data_counter_EncodeDateparts[month].replace({
        1:'January',2:'February',3:'March',4:'April',5:'May',6:'June',7:'July',
        8:'August',9:'September',10:'October',11:'November',12:'December'
        },inplace=True)
    selected_data_counter_EncodeDateparts[weekday].replace({
        0:'Monday',1:'Tuesday',2:'Wednesday',
        3:'Thurday',4:'Friday',5:'Saturday',6:'Sunday'},inplace=True)
    # selected_data_counter_EncodeDateparts[quarter].replace({
    #     1: 'Quarter-1',2: 'Quarter-2', 3: 'Quarter-3', 4: 'Quarter-4'
    #     },inplace=True)    
    selected_data_counter_EncodeDateparts[month]=selected_data_counter_EncodeDateparts[month].replace(np.nan,"Null").astype(str)
    selected_data_counter_EncodeDateparts[weekday]=selected_data_counter_EncodeDateparts[weekday].replace(np.nan,"Null").astype(str)

    return selected_data_counter_EncodeDateparts


#replace all empty values with hardcoded text <Null> so that we can give some analysis on the blank values
def replace_null_values(df):
    df = df.apply(lambda x: x.str.strip() if isinstance(x, str) else x).replace(np.nan, "Null")
    
    return df

#count no of tickets
def ticket_counter(df,imp_columns):
    
    df_counter = df.groupby(imp_columns).size().reset_index(name = 'counts')
    
    return df_counter

def getPluralForEntity(entity):
    engine= inflect.engine()
    return engine.plural(entity)


####################################################################

        
    

####################################################################
#time lapse analysis

def CreateYearlyNarrative(row,entity):
    if(row['PercentChange'] > 0 ):
        narrative= "Observed an '"+ str(round(row['PercentChange'],2)) + "' percent 'increase' in "+entity+" volume between '"+ \
        str(int(row[0])) +"' and '" + \
        str(int(row[0]) -1)+"'"
        
        narrative_df={'year1':int(row[0]),
             'year2':int(row[0])-1,
             'percentage':round(row['PercentChange'],2),'type':'increase','narratives':narrative
             }

    else:
        narrative= "Observed an '"+ str(-1*round(row['PercentChange'],2)) + "' percent 'decrease' in "+entity+" volume between '"+ \
        str(int(row[0])) +"' and '" + \
        str(int(row[0]) -1)+"'"
        
        narrative_df={'year1':int(row[0]),
             'year2':int(row[0])-1,
             'percentage':-1*round(row['PercentChange'],2),'type':'decrease','narratives':narrative
             }
    return narrative_df


def CreateYearlyQuarterNarrative(row,entity):
    if(row['PercentChange'] > 0 ):
        # narrative= "Observed an '"+ str(round(row['PercentChange'],2)) + "' percent 'increase' in "+entity+" volume between '"+ \
        #         str(int(row[0])) +"-Quarter " + str(int(row[1])) + \
        #         "' and '" + str(int(row[0]) -1)  +"-Quarter " + str(int(row[1]))+"'"
        narrative_list=["Observed an '"+ str(round(row['PercentChange'],2)) + "' percent 'increase' in "+entity+" volume between '"+ \
                                        str(int(row[0])) +"-Quarter " + str(int(row[1])) + \
                                        "' and '" + str(int(row[0]) -1)  +"-Quarter " + str(int(row[1]))+"'",
                        "For the interval between '"+ \
                                        str(int(row[0])) +"-Quarter " + str(int(row[1])) + \
                                        "' and '" + str(int(row[0]) -1)  +"-Quarter " + str(int(row[1]))+"', an '"+ str(round(row['PercentChange'],2)) + "' percent 'increase' in "+entity+" volume is noticed",
                        "'"+ str(round(row['PercentChange'],2)) + "' percent 'increase' in "+entity+" volume is detetcted for the duration between '"+ \
                                        str(int(row[0])) +"-Quarter " + str(int(row[1])) + \
                                        "' and '" + str(int(row[0]) -1)  +"-Quarter " + str(int(row[1]))+"'"]
        narrative=random.choice(narrative_list)
        narrative_df={'year1':int(row[0]),'year1_quarter':int(row[1]),
             'year2':int(row[0])-1,'year2_quarter':int(row[1]),
             'percentage':round(row['PercentChange'],2),'type':'increase','narratives':narrative
             }

    elif(row['PercentChange'] <0 ):
        # narrative= "Observed an '"+ str(-1*round(row['PercentChange'],2)) + "' percent 'decrease' in "+entity+" volume between '"+ \
        #         str(int(row[0])) +"-Quarter " + str(int(row[1])) + \
        #         "' and '" + str(int(row[0]) -1)  +"-Quarter " + str(int(row[1]))+"'"
        narrative_list=["Observed an '"+ str(-1*round(row['PercentChange'],2)) + "' percent 'decrease' in "+entity+" volume between '"+ \
                                    str(int(row[0])) +"-Quarter " + str(int(row[1])) + \
                                    "' and '" + str(int(row[0]) -1)  +"-Quarter " + str(int(row[1]))+"'",
                    "For the interval between '"+ \
                                    str(int(row[0])) +"-Quarter " + str(int(row[1])) + \
                                    "' and '" + str(int(row[0]) -1)  +"-Quarter " + str(int(row[1]))+"', an '"+ str(-1*round(row['PercentChange'],2)) + "' percent 'decrease' in "+entity+" volume is noticed",
                    "'"+ str(-1*round(row['PercentChange'],2)) + "' percent 'decrease' in "+entity+" volume is detetcted for the duration between '"+ \
                                    str(int(row[0])) +"-Quarter " + str(int(row[1])) + \
                                    "' and '" + str(int(row[0]) -1)  +"-Quarter " + str(int(row[1]))+"'"]
        narrative=random.choice(narrative_list)
        narrative_df={'year1':int(row[0]),'year1_quarter':int(row[1]),
             'year2':int(row[0])-1,'year2_quarter':int(row[1]),
             'percentage':-1*round(row['PercentChange'],2),'type':'decrease','narratives':narrative
             }
    else:
        # narrative= "Observed no change in "+entity+" volume between '"+ \
        #         str(int(row[0])) +"-Quarter " + str(int(row[1])) + \
        #         "' and '" + str(int(row[0]) -1)  +"-Quarter " + str(int(row[1]))+"'"
        narrative_list=["Observed no change in "+entity+" volume between '"+ \
                                        str(int(row[0])) +"-Quarter " + str(int(row[1])) + \
                                        "' and '" + str(int(row[0]) -1)  +"-Quarter " + str(int(row[1]))+"'",
                        "No change in number of "+getPluralForEntity(entity)+" is observed for time duartion between '"+ \
                                        str(int(row[0])) +"-Quarter " + str(int(row[1])) + \
                                        "' and '" + str(int(row[0]) -1)  +"-Quarter " + str(int(row[1]))+"'"]
        narrative=random.choice(narrative_list)
        narrative_df={'year1':int(row[0]),'year1_quarter':int(row[1]),
             'year2':int(row[0])-1,'year2_quarter':int(row[1]),
             'percentage':0,'type':'no change','narratives':narrative
             }
        
    

    
    return narrative_df

def CreateYearlyMonthlyNarrative(row,entity):
    narrative_df={'year1':int(row[0]),'year1_month':calendar.month_name[int(row[1])],
             'year2':int(row[0])-1,'year2_month':calendar.month_name[int(row[1])]
             }


    if(row['PercentChange'] > 0 ):
        # narrative= "Observed an '"+ str(round(row['PercentChange'],2)) + "' percent 'increase' in "+entity+" volume between '"+ \
        #         str(int(row[0])) +"-" + calendar.month_name[int(row[1])] + \
        #         "' and '" + str(int(row[0]) -1)  +"-" + calendar.month_name[int(row[1])]+"'"
        narrative_list=["Observed an '"+ str(round(row['PercentChange'],2)) + "' percent 'increase' in "+entity+" volume between '"+ \
                                        str(int(row[0])) +"-" + calendar.month_name[int(row[1])] + \
                                        "' and '" + str(int(row[0]) -1)  +"-" + calendar.month_name[int(row[1])]+"'",				
                        "For the interval between '"+ \
                                        str(int(row[0])) +"-" + calendar.month_name[int(row[1])] + \
                                        "' and '" + str(int(row[0]) -1)  +"-" + calendar.month_name[int(row[1])]+"', an '"+ str(round(row['PercentChange'],2)) + "' percent 'increase' in "+entity+" volume is noticed",
                        "'"+ str(round(row['PercentChange'],2)) + "' percent 'increase' in "+entity+" volume is detetcted for the duration between '"+ \
                                        str(int(row[0])) +"-" + calendar.month_name[int(row[1])] + \
                                        "' and '" + str(int(row[0]) -1)  +"-" + calendar.month_name[int(row[1])]+"'"]
        narrative=random.choice(narrative_list)
        narrative_df['percentage']=round(row['PercentChange'],2)
        narrative_df['type']='increase'
        narrative_df['narratives']=narrative
    elif (row['PercentChange'] < 0 ) :
        # narrative= "Observed an '"+ str(-1*round(row['PercentChange'],2)) + "' percent 'decrease' in "+entity+" volume between '"+ \
        #         str(int(row[0])) +"-" + calendar.month_name[int(row[1])] + \
        #         "' and '" + str(int(row[0]) -1)  +"-" + calendar.month_name[int(row[1])]+"'"
        narrative_list=["Observed an '"+ str(-1*round(row['PercentChange'],2)) + "' percent 'decrease' in "+entity+" volume between '"+ \
                                        str(int(row[0])) +"-" + calendar.month_name[int(row[1])] + \
                                        "' and '" + str(int(row[0]) -1)  +"-" + calendar.month_name[int(row[1])]+"'",				
                        "For the interval between '"+ \
                                        str(int(row[0])) +"-" + calendar.month_name[int(row[1])] + \
                                        "' and '" + str(int(row[0]) -1)  +"-" + calendar.month_name[int(row[1])]+"', an '"+ str(-1*round(row['PercentChange'],2)) + "' percent 'decrease' in "+entity+" volume is noticed",
                        "'"+ str(-1*round(row['PercentChange'],2)) + "' percent 'decrease' in "+entity+" volume is detetcted for the duration between '"+ \
                                        str(int(row[0])) +"-" + calendar.month_name[int(row[1])] + \
                                        "' and '" + str(int(row[0]) -1)  +"-" + calendar.month_name[int(row[1])]+"'"]
        narrative=random.choice(narrative_list)
        narrative_df['percentage']=-1*round(row['PercentChange'],2)
        narrative_df['type']='decrease'
        narrative_df['narratives']=narrative
    else:
        # narrative= "Observed no change in "+entity+" volume between '"+ \
        #         str(int(row[0])) +"-" + calendar.month_name[int(row[1])] + \
        #         "' and '" + str(int(row[0]) -1)  +"-" + calendar.month_name[int(row[1])]+"'"
        narrative_list=["Observed no change in "+entity+" volume between '"+ \
                                        str(int(row[0])) +"-" + calendar.month_name[int(row[1])] + \
                                        "' and '" + str(int(row[0]) -1)  +"-" + calendar.month_name[int(row[1])]+"'",
                        "No change in number of "+getPluralForEntity(entity)+" is observed for time duartion between "+ \
                                        str(int(row[0])) +"-" + calendar.month_name[int(row[1])] + \
                                        "' and '" + str(int(row[0]) -1)  +"-" + calendar.month_name[int(row[1])]+"'"]
        narrative=random.choice(narrative_list)
        narrative_df['percentage']=0
        narrative_df['type']='no change'
        narrative_df['narratives']=narrative
        
    
    return narrative_df
        
#df1=selected_data_counter.copy()
#date_args=[year,month]
#entity='sales'
def getNarrativesByDate(df1,entity,*date_args):  
#Group by Year to get year wide analysis
    date=[]
    for i in date_args:
        date.append(i)
    grouped_df = df1.groupby(date)
    grouped_and_summed = grouped_df['counts'].sum()
    grouped_and_summed = grouped_and_summed.reset_index()
    date_df = grouped_and_summed.copy()
    date_df = date_df.sort_values(by = date)
    date_df_for_trend=date_df.copy()
    
    if date[-1].endswith('Year'):
        date_df['PercentChange'] = date_df.pct_change()['counts'] * 100
        final_df = date_df.dropna()
    else:        
        temp_date={}
        flag=date[-1]
        unique_val=list(date_df[flag].unique())
        for i in unique_val:
            temp_date[i]=date_df[date_df[flag]==i]
            temp_date[i]=temp_date[i].sort_values(by=date)
        final_df=pd.DataFrame()
        for key,df in temp_date.items():
            df['PercentChange'] = df.pct_change()['counts'] * 100
            final_df=final_df.append(df)
        final_df=final_df.dropna()
    
    if final_df.empty:
        narrative_df=None
    else:
        
        if date[-1].endswith('Year'):
        
            narrative_df = pd.DataFrame.from_records(list(final_df.apply(lambda row: CreateYearlyNarrative(row,entity), axis=1)))
        elif date[-1].endswith('Quarter'):

            narrative_df = pd.DataFrame.from_records(list(final_df.apply(lambda row: CreateYearlyQuarterNarrative(row,entity), axis=1)))
        else:

            narrative_df = pd.DataFrame.from_records(list(final_df.apply(lambda row: CreateYearlyMonthlyNarrative(row,entity), axis=1)))

    return narrative_df,date_df_for_trend



'''
 #this code is to add just in text for bottom_5[ for future use,  not implemented fully]
 
def top5_bottom_5_Distribution(df):
    df.sort_values(by=['percentage'],inplace=True, ascending=False)
    top_5=df.head()
    top_5["narratives"]=top_5.apply(lambda row: generateDistributionNarrative_top5(row,entity), axis=1)
    bottom_5=df.tail()
    bottom_5["narratives"]=bottom_5.apply(lambda row: generateDistributionNarrative_bottom5(row,entity), axis=1)
    top_columns=top_5.append(bottom_5,ignore_index=True)
    topcolumns=top_columns.drop_duplicates(subset=list(top_columns.columns)[:-1])
    return topcolumns




def generateDistributionNarrative_top5(row,entity):
#    return(row['Service Type'])
     # str(row['percentage']) + " percent of "+entity+" volume belong to " \
     #            + str(row['col_a']) + " = '" + str(row['col_a_value'])+"'"

      return "'"+str(row['col_a']) + "' being '" + str(row['col_a_value'])+"' contributes to "+str(round(row['percentage'],2))+" percent of "+entity+" volume"
  
def generateDistributionNarrative_bottom5(row,entity):
#    return(row['Service Type'])
     # str(row['percentage']) + " percent of "+entity+" volume belong to " \
     #            + str(row['col_a']) + " = '" + str(row['col_a_value'])+"'"

      return "'"+str(row['col_a']) + "' being '" + str(row['col_a_value'])+"' contributes to just "+str(round(row['percentage'],2))+" percent of "+entity+" volume"

'''

def generateDistributionNarrative(row,entity):
   
     #return "'"+str(round(row['percentage'],2))+"' percent of "+entity+" have '"+str(row['col_a']) + "' as '" + str(row['col_a_value'])+"'"
     narrative_list=["'"+str(round(row['percentage'],2))+"' percent of "+entity+" have '"+str(row['col_a']) + "' as '" + str(row['col_a_value'])+"'",
                     "Observed value of '"+str(row['col_a']) + "' is found to be '" + str(row['col_a_value'])+"' for '"+str(round(row['percentage'],2))+"' percent of "+entity]
     return random.choice(narrative_list)
#df=selected_data_counter_EncodeDateparts.copy()
def getDistributionNarratives(df,entity):
    selectedDataCounterWithOutDateparts=df.copy()
    distributionNarrative=pd.DataFrame()
    columns = selectedDataCounterWithOutDateparts.columns.tolist()
    columns.remove('counts')
    for col in columns:
        grouped_df = selectedDataCounterWithOutDateparts.groupby([col]).agg({'counts': 'sum'})
        state_pcts = grouped_df.apply(lambda x:100 * x / float(x.sum())).reset_index(drop=False)
        for index, row in  state_pcts.iterrows():
            distributionNarrative = distributionNarrative.append(
                {'col_a':row.index[0],'col_a_value':row[0],
                 'percentage':round(row[1],2)} ,ignore_index=True)
    
    dN=distributionNarrative[distributionNarrative['percentage'].between(5,99, inclusive=True)]

    final_df=top5_bottom_5(dN)        
    #distributionNarrative = distributionNarrative.sort_values('percentage', ascending = False).reset_index(drop=True).head(10)
    final_df['narratives'] = final_df.apply(lambda row: generateDistributionNarrative(row,entity), axis=1)
  
    return final_df













def CorrelationNarrative(row,entity):
        
      #return "'"+str(round(row['percentage'],2))+"' percent of "+entity+" have '"+str(row['col_a']) + "' as '" + str(row['col_a_value'])+"' and '"+str(row['col_b']) +"' as '" + str(row['col_b_value'])+"'"
      #<KPI> percent of <entity> have <Col1Name> as '<Col1value>' and <Col2Name> as '<Col2value>'
      narrative_list=["'"+str(round(row['percentage'],2))+"' percent of "+entity+" have '"+str(row['col_a']) + "' as '" + str(row['col_a_value'])+"' and '"+str(row['col_b']) +"' as '" + str(row['col_b_value'])+"'",
                      "Observed value of  '"+str(row['col_a']) + "' is found to be '" + str(row['col_a_value'])+"' and '"+str(row['col_b']) +"' is found to be as '" + str(row['col_b_value'])+"' for '"+str(round(row['percentage'],2))+"' percent of "+entity]
      return random.choice(narrative_list)
# selectedDataCounterWithOutDateparts=selected_data_counter_EncodeDateparts.copy()
def getNarrativebyCorrelationbetweenColumns(df,entity):
    selectedDataCounterWithOutDateparts=df.copy()
    correlationNarrative = pd.DataFrame()
    correlations = {}
    cat_col_list={}
    columns = selectedDataCounterWithOutDateparts.columns.tolist()
    numerical_columns= selectedDataCounterWithOutDateparts.select_dtypes(include=np.number).columns.tolist()
    for i in numerical_columns:
        cat_col_list[i]=i
    #convert object type columns so that we can get correlation
    for col in selectedDataCounterWithOutDateparts.columns:
        col_datatype=str(selectedDataCounterWithOutDateparts[col].dtype)
        if  col_datatype== 'object' or col_datatype == 'bool':
            try:
                selectedDataCounterWithOutDateparts[col+"_cat"] = selectedDataCounterWithOutDateparts[col].astype('category').cat.codes
                cat_col_list[col+"_cat"]=col #this dict is used for decoding columns
                columns.append(col+"_cat")
                columns.remove(col)
            except ValueError:
                    error_encounterd = "Valueerror"
        

    #calculate pearson correlation
    columns.remove('counts')
    cat_col_list.pop('counts')

    for col_a, col_b in itertools.combinations(columns, 2):
        correlations[cat_col_list[col_a] + '__' + cat_col_list[col_b]] = {'Col1':cat_col_list[col_a], 'Col2':cat_col_list[col_b], 
                'Corr':pearsonr(selectedDataCounterWithOutDateparts.loc[:, col_a], 
                                selectedDataCounterWithOutDateparts.loc[:, col_b])[0]}

    correlationresult = pd.DataFrame.from_dict(correlations, orient='index')
    correlationresult.reset_index(drop = True)

    #correlationresult.to_excel("correlationresult.xlsx")

    selectedsortedCorrMatrix = correlationresult.loc[((correlationresult['Corr'] > 0.6) & 
                                                  (correlationresult['Corr'] < 0.999999)) | 
    (correlationresult['Corr'] < -0.6)]

    #selectedsortedCorrMatrix.to_excel("selectedsortedCorrMatrix.xlsx")
    #selectedDataCounterWithOutDateparts.drop(cat_col_list,axis=1,inplace=True)
    # correlationNarrativeColumns = []
    # correlationNarrativeColumns.append('Narrative')
    # correlationNarrative = pd.DataFrame(columns = correlationNarrativeColumns)
    for index, CorrColumnrow in selectedsortedCorrMatrix.iterrows():
        grouped_df = selectedDataCounterWithOutDateparts.groupby([CorrColumnrow['Col1'], CorrColumnrow['Col2']]).agg({'counts': 'sum'})
        state_pcts = grouped_df.apply(lambda x:100 * x / float(x.sum())).reset_index(drop=False)
        for index, row in  state_pcts.iterrows():
            correlationNarrative = correlationNarrative.append(
                {'col_a':row.index[0],'col_a_value':row[0],
                 'col_b':row.index[1],'col_b_value':row[1],
                 'percentage':round(row[2],2)} ,ignore_index=True)
    
    if len(correlationNarrative)==0:
        return None
    else:
        cN = correlationNarrative[correlationNarrative['percentage'].between(5,99, inclusive=True)]
        final_corr_df=top5_bottom_5(cN)
        final_corr_df['narratives'] = final_corr_df.apply(lambda row: CorrelationNarrative(row,entity), axis=1)
    
        return final_corr_df



# =============================================================================
# #metric is continous
# =============================================================================

def filter_impfeatures(dim_df,list_all):
    dim_df_copy=dim_df.copy()
    #list_all=list(dim_df_copy['Feature'])
    
    corr_dict={}
    for col_a, col_b in itertools.combinations(list_all, 2):
    
        corr_value=pearsonr(dim_df_copy.loc[:, col_a], dim_df_copy.loc[:, col_b])[0]
    
        corr_dict[col_a+"_"+col_b]={"cola":col_a,"colb":col_b,"corr":corr_value}
        if ((corr_value> 0.85) or (corr_value <-0.85)):
            try:
                list_all.remove(col_b)
            except ValueError:
                error_encounterd = "Valueerror"
    corr_df=pd.DataFrame.from_dict(corr_dict,orient='index') #this df is for reference
    return list_all[:15] 

    

def get_important_dim_by_regression(selectData_wo_dates_Regression,target):
    selectData_wo_dates_Regression_models=selectData_wo_dates_Regression.copy()
    X=selectData_wo_dates_Regression_models.drop(target,axis=1)
    #X=X.drop('counts',axis=1)
    y=selectData_wo_dates_Regression_models [target]
    le= LabelEncoder()
    for i in X.columns:
        if str(X[i].dtype) in ["object","bool"]:
            X[i] = le.fit_transform(selectData_wo_dates_Regression_models[i].astype(str))
  
    dim_df= pd.DataFrame(columns=['Feature','score'])


    from sklearn.ensemble import RandomForestRegressor
    model = RandomForestRegressor()
    # fit the model
    model.fit(X, y)
    # get importance
    importance = model.feature_importances_
    # summarize feature importance
    for i,v in enumerate(importance):
        dim_df.loc[len(dim_df.index)] = [X.columns[i],v] 
    
    dim_df = dim_df.sort_values(by=['score'],ascending=False)
    dim_df.reset_index(drop=True,inplace=True)
    all_list=list(dim_df['Feature'])
    top10_dim_list_regression = filter_impfeatures(X,all_list)
    all_columns=list(X.columns)  #used to find connected dimensions in chi2 test
    return top10_dim_list_regression,all_columns
 
    
def get_important_dim_by_linear_regression(selectData_wo_dates_Regression,target):
    selectData_wo_dates_Regression_models=selectData_wo_dates_Regression.copy()
    X=selectData_wo_dates_Regression_models.drop(target,axis=1)
    #X=X.drop('counts',axis=1)
    y=selectData_wo_dates_Regression_models [target]
    le= LabelEncoder()
    for i in X.columns:
        if X[i].dtype =="object":
            X[i] = le.fit_transform(selectData_wo_dates_Regression_models[i].astype(str))
  
    dim_df= pd.DataFrame(columns=['Feature','score'])


    from sklearn.linear_model import LinearRegression
    model = LinearRegression()
    # fit the model
    model.fit(X, y)
    # get importance
    importance = model.coef_
    # summarize feature importance
    for i,v in enumerate(importance):
        dim_df.loc[len(dim_df.index)] = [X.columns[i],v] 
    
    dim_df = dim_df.sort_values(by=['score'],ascending=False)
    dim_df.reset_index(drop=True,inplace=True)   
    all_list=list(dim_df['Feature'])
    top10_dim_list_regression = filter_impfeatures(X,all_list)
    all_columns=list(X.columns)  #used to find connected dimensions in chi2 test
    return top10_dim_list_regression,all_columns

 
    


def get_important_dim_by_anova(selectedData_wo_Dates_Anova,target):
    prob=0.95
    alpha=1-prob
    selectedData_wo_Dates_Anova_all=selectedData_wo_Dates_Anova.copy()
    #renaming columns
    anova_columns=selectedData_wo_Dates_Anova_all.columns
    modified_column={}
    #replacing space and special chars with '_'  because spaces and special chars gives error in anova test
    for i in anova_columns:
        modified_column[i]=re.sub("[-() ]","_",i)
        selectedData_wo_Dates_Anova_all.rename(columns={i:re.sub("[-() ]","_",i)}, inplace=True)
    
    #renamed target column
    target_anova=modified_column.pop(target)

    #generating formula for anova[formula example: target~C(col)+C(col2)+ ____  so on]
    dict_target_pvalue={}
    formula_columns={}

    for key,value in modified_column.items():
        #if col is categorical 'C' should place before col like C(col)
        if selectedData_wo_Dates_Anova_all[value].dtype =="object":
            formula_columns[key]="C("+value+")"
        
        else:
        #this is the case for continous column  
        #In our case continous columns are removed  so this case can be exculded
            formula_columns[key]=value
        
    temp_column_val="+".join(i for i in formula_columns.values()   if   i !=target_anova   )
        
        
    formula1=target_anova+"~"+temp_column_val
        #applying anova model
    model1  = ols(formula1,selectedData_wo_Dates_Anova_all).fit()
    val1=sm.stats.anova_lm(model1,type="2")  
        #slicing val1 for P values
    pvalue_df=val1.iloc[:-1,:] #removing Residual row 
    
    #selecting the columns which has p value <0.05 (alpha)
    temp_list_dimensions_anova_all=[]

    for i in range(0,pvalue_df.shape[0]):
        if pvalue_df.iloc[i,-1] < alpha:
            temp_list_dimensions_anova_all.append(pvalue_df.index[i])
    
    #getting the orginal columns names  using formula_columns
    list_dimensions_anova_all=[] #this  list is used for  finding connected dimensions in chi2 test

    for i in temp_list_dimensions_anova_all:
        for l,m in formula_columns.items():
            if m==i:
                list_dimensions_anova_all.append(l)


    #based on P value (more lesser more better)  top  columns are selected
    dimension_pvalue_df_anova_all=pvalue_df.loc[temp_list_dimensions_anova_all,:]
    
    #sorting with p value  

    dimension_pvalue_df=dimension_pvalue_df_anova_all.sort_values(by='PR(>F)')
    
    
    #renaming top columns into original
    temp_dim_list=list(dimension_pvalue_df.index)
    all_list=[]
    for i in temp_dim_list:
        for l,m in formula_columns.items():
            if m==i:
                all_list.append(l)
    
    #all_list=list(dimension_pvalue_df.index)
    selectedData_wo_Dates_Anova_corr=selectedData_wo_Dates_Anova.copy()
     #convert object type columns so that we can get correlation
    for col in selectedData_wo_Dates_Anova_corr.columns:
        if selectedData_wo_Dates_Anova_corr[col].dtype == 'object':
            try:
                selectedData_wo_Dates_Anova_corr[col] = selectedData_wo_Dates_Anova_corr[col].astype('category').cat.codes
            except ValueError:
                error_encounterd = "ValueError"
    
    top10_dim_list_all=filter_impfeatures(selectedData_wo_Dates_Anova_corr, all_list)
    
    

    return top10_dim_list_all,list_dimensions_anova_all

    
# =============================================================================
# chi2 test
# 
# =============================================================================

#chi square test for connected dim n's
#link for ref:https://machinelearningmastery.com/chi-squared-test-for-machine-learning/

def chi2_test_for_conn_dims(data_frame,top_dim_list_all,list_dimensions):
    prob=0.95
    alpha=1-prob

    connected_dimns_dict_all={}
    for dim in top_dim_list_all:
        temp_connected_dimns=pd.DataFrame(columns=["p_value","chi2_value"])
        for v in list_dimensions:
            if dim!=v:
                data_table=pd.crosstab(data_frame[dim],data_frame[v])   
                observe_values=data_table.values
                chi2_value,p,dof,expected=stats.chi2_contingency(observe_values)
                critical = stats.chi2.ppf(prob, dof)
                if p<=alpha or chi2_value>=critical:
                    temp_connected_dimns.loc[v]=[p,chi2_value]
        temp_connected_dimns.sort_values(by = ['p_value','chi2_value'],inplace=True, ascending=False)
        top2_connected_dimns=list(temp_connected_dimns.head(2).index)  
        connected_dimns_dict_all[dim]=top2_connected_dimns 
    connected_dimns_df_all=pd.DataFrame.from_dict(connected_dimns_dict_all,orient='index',columns=["connected_dimn_1","connected_dimn_2"])
    return connected_dimns_df_all

# data_frame=selectData_wo_dates_for_continousmetric.copy()
# top_dim_list_all= imp_features
# list_dimensions=list_of_columns
def chi2_cramersV_test_for_conn_dims(data_frame,top_dim_list_all,list_dimensions):
    prob=0.95
    alpha=1-prob
    connected_dimns_dict_all={}
    for dim in top_dim_list_all:
        temp_connected_dimns=pd.DataFrame(columns=["p_value","chi2_value","Cramers_V"])
        for v in list_dimensions:
            if dim!=v:
                data_table=pd.crosstab(data_frame[dim],data_frame[v])
                observe_values=data_table.values
                n=data_frame.shape[0]
                chi2_value,p,dof,expected=stats.chi2_contingency(observe_values)
                critical = stats.chi2.ppf(prob, dof)
                if p<=alpha or chi2_value>=critical:
                    min_val=min(data_table.shape)  #  the lesser number of categories of either variable.
                    Cramers_V=np.sqrt(chi2_value/(n*(min_val-1))) # refer for formula https://www.spss-tutorials.com/cramers-v-what-and-why/
                    temp_connected_dimns.loc[v]=[p,chi2_value,Cramers_V]
        temp_connected_dimns.sort_values(by = ['Cramers_V','chi2_value'],inplace=True, ascending=False)
        
        temp_connected_dimns = temp_connected_dimns.loc[(temp_connected_dimns['Cramers_V'] < 0.9)]

        
        
        #top2_connected_dimns=list(temp_connected_dimns.head(2).index)
        top2_connected_dimns=[]
        temp_connected_dimns_copy=temp_connected_dimns.copy()
        try:
            connected_1_df=temp_connected_dimns.head(1)
            top2_connected_dimns.append(connected_1_df.index[0])
        except Exception:
            top2_connected_dimns.append(np.nan)

        #selecting connected dim 2 which have different cramers v value with connected dim
        #this condition is taken not to select the same corrlated columns as connected dimns
        try:
            connected_2_df=temp_connected_dimns[temp_connected_dimns['Cramers_V']!=connected_1_df['Cramers_V'].values[0]].head(1)
            top2_connected_dimns.append(connected_2_df.index[0])
        except Exception:
            top2_connected_dimns.append(np.nan)

        connected_dimns_dict_all[dim]=top2_connected_dimns
    connected_dimns_df_all=pd.DataFrame.from_dict(connected_dimns_dict_all,orient='index',columns=["connected_dimn_1","connected_dimn_2"])

    return connected_dimns_df_all
                
#####################################################################

def connectedFeaturesConversionToStr(df1):               
    dataFrame=df1.copy()
    #listcombinations=[]
    combinations_df=pd.DataFrame()
    #dF=dataFrame.dropna(subset=['connected_dimn_1'])
    for key,value in dataFrame.iterrows():
        if value['connected_dimn_1'] is not None and value['connected_dimn_1'] is not None:
            
            combinationstr=key+" -- "+str(value["connected_dimn_1"])+" -- "+str(value["connected_dimn_2"])
            #listcombinations.append(combinationstr)
            combinations_df=combinations_df.append({"FeatureName":key,"ConnectedFeatures":combinationstr},ignore_index=True)
        else:
            combinations_df=combinations_df.append({"FeatureName":key,"ConnectedFeatures":None},ignore_index=True)

    combinations_dict=list(combinations_df.to_dict(orient="records"))
    return combinations_dict

"""
combinationsdf=connected_df.copy()
"""
def connectedFeaturesConversionToDF(combinationsdf):  
    combinations_copy=combinationsdf.copy()
    if combinations_copy.empty:
        return None
    else:
        connectedfeat_df=pd.DataFrame()
        for key,val in combinations_copy.iterrows():
            if val['ConnectedFeatures'] is not None:
                arr=val['ConnectedFeatures'].split(" -- ")
                if arr[2]=="None":
                    arr[2]=None
                connectedfeat_df=connectedfeat_df.append({"Index":arr[0],"connected_dimn_1":arr[1],"connected_dimn_2":arr[2]},ignore_index=True)
        connectedfeat_df.set_index(['Index'],inplace=True)
        return connectedfeat_df



# =============================================================================
# Outlier function 
# =============================================================================


def std_dev(data):
    # Number of observations
    n = len(data)
    # Mean of the data
    mean = sum(data) / n
    # Square deviations
    deviations = [(x - mean) ** 2 for x in data]
    # Variance
    variance = sum(deviations) / n
    sd = math.sqrt(variance)
    return sd

def z_score_calculate(data,popmean,popsd):
    temp_zscore=[]
    temp_zscore=(data-popmean)/popsd
    return temp_zscore
    
# df=grouped_summed_df.copy()
# popmean=total_target_mean
# popsd=overall_sd   
                 
def outlier_function(df,popmean,popsd):
    temp_df=df.copy()

    temp_df['target_zscore']=z_score_calculate(temp_df['average'],popmean,popsd)
    #temp_df.to_excel('temp_df.xlsx')

    for index,columnrows in temp_df.iterrows():
        if columnrows['target_zscore'] <3 and columnrows['target_zscore'] >-3:
            temp_df.drop(index,inplace=True,axis=0)
    if temp_df.empty==True:
        return pd.Series()
    else:
        #outlier_narr_temp = temp_df.apply(lambda row: CreateOutlierNarrative(row), axis=1) 
        return temp_df 
    
def CreateOutlierNarrative(row):
    col0=row.index[0]#dim1
    col1=row.index[1]#dim1_value
    col2=row.index[2]#metric
    col3=row.index[3]#metric_value
    #col4=row.index[4]
    narrative= "Unusual Average '"+str(row[col2])+"' value of '"+str(round(row[col3],2))+"' is observed when '"+str(row[col0])+"' is '"+str(row[col1])+"'"

    #narrative=  "Unusual Average "+str(col1)+" = "+str(round(row[col3],2))+" is observed when "+ \
    #            str(col0) +" = '"+str(row[col0])+"'"
    outlier_narr={
            'metric':row[col2],'metric_value':round(row[col3],2),'type':'Unusual','dim1':row[col0],'dim1_value':row[col1],
            'narratives':narrative
            }
    return outlier_narr

 
   
def outlier_function_one_connected(df,popmean,popsd):
    
    temp_df=df.copy()
    temp_df['target_zscore']=z_score_calculate(temp_df['average'],popmean,popsd)
    #temp_df.to_excel('temp_df.xlsx')

    for index,columnrows in temp_df.iterrows():
        if columnrows['target_zscore'] <3 and columnrows['target_zscore'] >-3:
            temp_df.drop(index,inplace=True,axis=0)
    if temp_df.empty==True:
        return pd.Series()
    else:
        #outlier_narr_temp = temp_df.apply(lambda row: CreateOutlierNarrative_one_connected(row), axis=1) 
        return temp_df 

    

    
def CreateOutlierNarrative_one_connected(row):
    col0=row.index[0]#dim1
    col1=row.index[1]#dim1_value
    col2=row.index[2]#dim2
    col3=row.index[3]#dim2_value
    col4=row.index[4]#metric
    col5=row.index[5]#metric_value
    
    narrative="Unusual Average '"+str(row[col4])+"' value of '"+str(round(row[col5],2))+"' is observed when '"+str(row[col0])+"' is '"+str(row[col1])+"' and '"+str(row[col2]) +"' is '"+str(row[col3])+"'"
    
    # narrative=  "Unusual Average "+str(col2)+" = "+str(round(row[col4],2))+" is observed when "+ \
    #             str(col0) +" = '"+str(row[col0])+"' and "+str(col1) +" = '"+str(row[col1])+"'"
    outlier_narr={
            'metric':row[col4],'metric_value':round(row[col5],2),'type':'Unusual','dim1':row[col0],'dim1_value':row[col1],'dim2':row[col2],'dim2_value':row[col3],
            'narratives':narrative
            }
    return outlier_narr

def outlier_function_two_connected(df,popmean,popsd):
    temp_df=df.copy()
    temp_df['target_zscore']=z_score_calculate(temp_df['average'],popmean,popsd)
    for index,columnrows in temp_df.iterrows():
        if columnrows['target_zscore'] <3 and columnrows['target_zscore'] >-3:
            temp_df.drop(index,inplace=True,axis=0)
    if temp_df.empty==True:
        return pd.Series()
    else:
    
        #outlier_narr_temp = temp_df.apply(lambda row: CreateOutlierNarrative_two_connected(row), axis=1) 
        return temp_df 

def CreateOutlierNarrative_two_connected(row):
    col0=row.index[0]#dim1
    col1=row.index[1]#dim1_value
    col2=row.index[2]#dim2
    col3=row.index[3]#dim2_value
    col4=row.index[4]#dim3    
    col5=row.index[5]#dim3_value
    col6=row.index[6]#metric
    col7=row.index[7]#metric_value
    
    narrative="Unusual Average '"+str(row[col6])+"' value of '"+str(round(row[col7],2))+"' is observed when '"+str(row[col0])+"' is '"+str(row[col1])+"' , '"+str(row[col2]) +"' is '"+str(row[col3])+"' and '"+str(row[col4]) +"' is '"+str(row[col5])+"'"

    # narrative=  "Unusual Average "+str(col3)+" = "+str(round(row[col5],2))+" is observed when "+ \
    #             str(col0) +" = '"+str(row[col0])+"' , "+str(col1) +" = '"+str(row[col1])+"' and "+str(col2) +" = '"+str(row[col2])+"'"
    outlier_narr={
            'metric':row[col6],'metric_value':round(row[col7],2),'type':'Unusual','dim1':row[col0],'dim1_value':row[col1],'dim2':row[col2],'dim2_value':row[col3],'dim3':row[col4],'dim3_value':row[col5],
            'narratives':narrative
            }
    return outlier_narr


# =============================================================================
# end for Outlier function 
# =============================================================================



def top5_bottom_5(df):
    df.sort_values(by=['percentage'],inplace=True, ascending=False)
    top_5=df.head()
    
    bottom_5=df.tail()
    top_columns=top_5.append(bottom_5,ignore_index=True)
    topcolumns=top_columns.drop_duplicates()
    return topcolumns

#df=two_connected_dims_outliers_narratives_all.copy()
def top5_bottom_5_outliers(df):
    df.sort_values(by=['target_zscore'],inplace=True, ascending=False)
    postive_df=df[df['target_zscore'] >= 0]
    negative_df=df[df['target_zscore'] < 0]
    top_5=postive_df.head()
    bottom_5=negative_df.tail()
    top_columns=top_5.append(bottom_5,ignore_index=True)
    topcolumns=top_columns.drop_duplicates()
    return topcolumns



# =============================================================================
# one dimension narratives
# 
# =============================================================================

# ten_columns=ten_imp_dimns_by_regression
# target=continous_target
# df=selectData_wo_dates_for_continousmetric.copy()

def createNarrativeForImpFeatures(row):
    col0=row.index[0]
    col1=row.index[1]
    col2=row.index[2]
    col3=row.index[3]
    col4=row.index[4]
    row[col4]=round(row[col4],2)
    row['metric_value']=round(row['metric_value'],2)
    

    
    if row[col4] >= 0:
        narrative=   "Average '"+str(row[col2])+"' is '"+str(row[col4])+"%' 'more' when '" + str(row[col0]) + "' is '" + str(row[col1])+"'"           
        narrative1=   "Average '"+str(row[col2])+"' is '"+str(round(row[col4]/100,2)+1)+"X' times when '" + str(row[col0]) + "' is '" + str(row[col1])+"'"           

        type_temp='more'
        percentage=row[col4]
        x_times=round(row[col4]/100,2)+1
    else :
        
        narrative=   "Average '"+str(row[col2])+"' is '"+str(-1*row[col4])+"%' 'less' when '" + str(row[col0]) + "' is '" + str(row[col1])+"'"           
        narrative1=   "Average '"+str(row[col2])+"' is '"+str(round(row[col4]/100,2)+1)+"X' times when '" + str(row[col0]) + "' is '" + str(row[col1])+"'"           
        type_temp='less'
        percentage=-1*row[col4]
        x_times=round(row[col4]/100,2)+1
    narrative_dict={'dim1':row[col0],"dim1_value":row[col1],'metric':row[col2],'metric_value':round(row['metric_value'],2),'percentage':percentage,'X_times':x_times,'type':type_temp,'narratives':narrative} 
    return narrative_dict  

"""
df=selectData_continousmetric_narratives.copy()
target=targetfornarrative
ten_columns=imp_features
"""

def get_imp_dimension_narratives_and_outlier_narratives(df,target,ten_columns):
    data_df=df.copy()        
    dims_narratives_all=pd.DataFrame()
    imp_dims_outliers_narratives_all=pd.DataFrame()
    for col in ten_columns:
        try:
            grouped_conn_df=data_df.groupby(col)        
            grouped_counter =grouped_conn_df.size().reset_index(name='counts')
            grouped_summed_df=grouped_conn_df[target].sum().reset_index()        
            grouped_summed_df['counts']=grouped_counter['counts']
            total_target_mean=data_df[target].mean()
            grouped_summed_df["average"]=grouped_summed_df[target]/grouped_summed_df['counts']
            target_per=((grouped_summed_df['average']-total_target_mean)/total_target_mean)*100
            grouped_summed_df["percentage"]=target_per
            
                
            #grouped_summed_df.to_excel('grouped_summed_df.xlsx')
            #generating narratives for Outliers 
            overall_sd=std_dev(data_df[target])  #std_dev  function in outlier section
            narratives_series=outlier_function(grouped_summed_df,total_target_mean,overall_sd)
            if narratives_series.empty==False:
                #print(narratives_series.columns)
                for index,rows in narratives_series.iterrows(): 
                        #print(index,rows.index[0])
                       imp_dims_outliers_narratives_all= imp_dims_outliers_narratives_all.append({
                                'dim1':rows.index[0],'dim1_value':str(rows[0]),
                                'metric':rows.index[1],'metric_value':rows[3],
                                'target_zscore':rows['target_zscore']},ignore_index=True)            
                
                    
            
                
            #grouped_summed_df.sort_values(by=['percentage'],inplace=True, ascending=False)
            #generating narratives 
            #grouped_summed_df['Narrative'] = grouped_summed_df.apply(lambda row: createNarrativeForImpFeatures(row), axis=1)
            
            #appending narratives to dataframe
            for index, row in  grouped_summed_df.iterrows():
            #print(row[3])
                dims_narratives_all = dims_narratives_all.append({'dim':row.index[0],'dim_value':str(row[0]),'metric':row.index[1],'metric_value':row[3],'percentage':row['percentage']} ,ignore_index=True)
        except KeyError as e:
            error_key=e.args
    dims_narratives_all.sort_values(by=['percentage'],inplace=True, ascending=False)
    final_narratives_df=top5_bottom_5(dims_narratives_all)

    final_narratives_df = final_narratives_df.apply(lambda row: createNarrativeForImpFeatures(row), axis=1)
    if len(imp_dims_outliers_narratives_all)==0: #check if there  are no outliers then no of records  is zero
        outlier_narratives=None
    else:
        final_outliers=top5_bottom_5_outliers(imp_dims_outliers_narratives_all)
        final_outliers_df=final_outliers.apply(lambda row:CreateOutlierNarrative(row), axis=1)
        outlier_narratives=pd.DataFrame(list(final_outliers_df))
    return pd.DataFrame(list(final_narratives_df)),outlier_narratives 



     
            
# =============================================================================
# end for one dimesion narratives
# 
# =============================================================================




# =============================================================================
# one dimn with  2 conn dimns naratives  and outlier narratives   
#  
# =============================================================================

def CreateMetricDimensionNarrative_two_conn(row):
    col0=row.index[0]
    col1=row.index[1]
    col2=row.index[2]
    col3=row.index[3]
    col4=row.index[4]
    col5=row.index[5]
    col6=row.index[6]
    col7=row.index[7]
    col8=row.index[8]
    row[col8]=round(row[col8],2)
    if row[col8] >=0:
        type_temp='more'
        percentage=row[col8]
        x_times=round(row[col8]/100,2)+1
        
        narrative="Average '"+str(row[col6]) +"' is '"+str(row[col8])+"%' 'more' when '"+ \
            str(row[col0]) +"' is '"+str(row[col1])+"' , '"+str(row[col2]) +"' is '"+str(row[col3])+"' and '" +\
                  str(row[col4]) +"' is '"+str(row[col5])+"'"
        
        narrative1= "Average '"+str(row[col6]) +"' is '"+str(round(row[col8]/100,2)+1)+"X' times  when '"+ \
            str(row[col0]) +"' is '"+str(row[col1])+"' , '"+str(row[col2]) +"' is '"+str(row[col3])+"' and '" +\
                  str(row[col4]) +"' is '"+str(row[col5])+"'"
        
    else:
        type_temp='less'
        percentage=-1*row[col8]
        x_times=round(row[col8]/100,2)+1
        narrative="Average '"+str(row[col6]) +"' is '"+str(-1*row[col8])+"%' 'less' when '"+ \
            str(row[col0]) +"' is '"+str(row[col1])+"' ,'"+str(row[col2]) +"' is '"+str(row[col3])+"' and '" +\
                  str(row[col4]) +"' is '"+str(row[col5])+"'"
        
        narrative1= "Average '"+str(row[col6]) +"' is '"+str(round(row[col8]/100,2)+1)+"X' times  when '"+ \
            str(row[col0]) +"' is '"+str(row[col1])+"' , '"+str(row[col2]) +"' is '"+str(row[col3])+"' and '" +\
                  str(row[col4]) +"' is '"+str(row[col5])+"'"
        
    narrative_dict={'dim1':row[col0],"dim1_value":row[col1],
                    'dim2':row[col2],"dim2_value":row[col3],
                    'dim3':row[col4],"dim3_value":row[col5],
                    'metric':row[col6],'metric_value':round(row['metric_value'],2),'percentage':percentage,'X_times':x_times,'type':type_temp,'narratives':narrative} 
    return narrative_dict

# selectedData_wo_Dates_naratives=selectData_continousmetric_narratives.copy()
# connected_dimns_df=connected_dims.copy()        
def get_two_connected_narratives_and_outlier_narratives(selectedData_wo_Dates_naratives,target,connected_dimns_df):
    connected_dims_narratives_all=pd.DataFrame()
    two_connected_dims_outliers_narratives_all=pd.DataFrame()
    connected_dimns=connected_dimns_df.dropna()
    for key_dimn,column_values in connected_dimns.iterrows():
        try:
            #print(key_dimn,column_values['connected_dimn_1'],column_values['connected_dimn_2'])
            grouped_conn_df = selectedData_wo_Dates_naratives.groupby([key_dimn, column_values['connected_dimn_1'],column_values['connected_dimn_2']])
            grouped_counter =grouped_conn_df.size().reset_index(name='counts')
            grouped_summed_conn_df=grouped_conn_df[target].sum().reset_index()        
            grouped_summed_conn_df['counts']=grouped_counter['counts']
            total_target_mean=selectedData_wo_Dates_naratives[target].mean()
            grouped_summed_conn_df["average"]=grouped_summed_conn_df[target]/grouped_summed_conn_df['counts']
            target_per=((grouped_summed_conn_df['average']-total_target_mean)/total_target_mean)*100
            grouped_summed_conn_df["percentage"]=target_per
            #grouped_summed_conn_df.sort_values(by=['percentage'],inplace=True, ascending=False)
        
            #generating narratives for Outliers
            overall_sd=std_dev(selectedData_wo_Dates_naratives[target])  #std_dev  function in outlier section
            narratives_series=outlier_function_two_connected(grouped_summed_conn_df,total_target_mean,overall_sd)
            if narratives_series.empty==False:
                    for index,rows in narratives_series.iterrows(): 
                        #print(index,rows.index[0])
                       two_connected_dims_outliers_narratives_all= two_connected_dims_outliers_narratives_all.append({
                                'dim1':rows.index[0],'dim1_value':str(rows[0]),
                                'dim2':rows.index[1],'dim2_value':str(rows[1]),
                                'dim3':rows.index[2],'dim3_value':str(rows[2]),                                                        
                                'metric':rows.index[3],'metric_value':rows[5],
                                'target_zscore':rows['target_zscore']},ignore_index=True)            
                
            
            
            #appending narratives to dataframe
            for index, row in  grouped_summed_conn_df.iterrows():
                #print(row[3])
                connected_dims_narratives_all = connected_dims_narratives_all.append(
                    {'dim1':row.index[0],'dim1_value':str(row[0]),
                     'dim2':row.index[1],'dim2_value':str(row[1]),
                     'dim3':row.index[2],'dim3_value':str(row[2]),
                     'metric':row.index[3],'metric_value':row[5],
                     'percentage':row[6]} ,ignore_index=True)
    
        except KeyError as e:
            error_key=e.args
    final_grouped_df=top5_bottom_5(connected_dims_narratives_all)
    #generating narratives for connected dimensions
    final_grouped_df = final_grouped_df.apply(lambda row: CreateMetricDimensionNarrative_two_conn(row), axis=1)
    
    if len(two_connected_dims_outliers_narratives_all)==0: #check if there  are no outliers then no of records  is zero
        outlier_narratives=None
    else:
        final_outliers=top5_bottom_5_outliers(two_connected_dims_outliers_narratives_all)
        final_outliers_df=final_outliers.apply(lambda row: CreateOutlierNarrative_two_connected(row), axis=1)
        outlier_narratives=pd.DataFrame(list(final_outliers_df))

    return pd.DataFrame(list(final_grouped_df)),outlier_narratives




# =============================================================================
# one dimn with  one conn dimns naratives  and outlier narratives   
#  
# =============================================================================

def CreateMetricDimensionNarrative_one_conn(row):
    col0=row.index[0]#dim1
    col1=row.index[1]#dim1_value
    col2=row.index[2]#dim2
    col3=row.index[3]#dim2_value
    col4=row.index[4]#metric
    col5=row.index[5]#metric_value
    col6=row.index[6]#percentage
    row[col6]=round(row[col6],2)
    if row[col6] >= 0 :
        percentage=row[col6]
        type_temp='more'
        x_times=round(percentage/100,2)+1
        narrative="Average '"+str(row[col4]) +"' is '"+str(row[col6])+"%' 'more' when '"+ \
            str(row[col0]) +"' is '"+str(row[col1])+"' and '"+str(row[col2]) +"' is '"+str(row[col3])+"'"
        
        narrative1="Average '"+str(row[col4]) +"' is '"+str(x_times)+"X' times  when '"+ \
            str(row[col0]) +"' is '"+str(row[col1])+"' and '"+str(row[col2]) +"' is '"+str(row[col3])+"'"
        
    else:
        percentage=-1*row[col6]
        type_temp='less'
        x_times=round(row[col6]/100,2)+1

        narrative="Average '"+str(row[col4]) +"' is '"+str(-1*row[col6])+"%' 'less' when '"+ \
            str(row[col0]) +"' is '"+str(row[col1])+"' and '"+str(row[col2]) +"' is '"+str(row[col3])+"'"
        
        narrative1="Average '"+str(row[col4]) +"' is '"+str(x_times)+"X' times  when '"+ \
            str(row[col0]) +"' is '"+str(row[col1])+"' and '"+str(row[col2]) +"' is '"+str(row[col3])+"'"
        
    
    narrative_dict={'dim1':row[col0],"dim1_value":row[col1],
                    'dim2':row[col2],"dim2_value":row[col3],                    
                    'metric':row[col4],'metric_value':row['metric_value'],'percentage':percentage,'X_times':x_times,'type':type_temp,'narratives':narrative} 
    return narrative_dict

# selectedData_wo_Dates_naratives=selectData_wo_dates_for_continousmetric
# target=targetfornarrative
# connected_dimns_df=connected_dims

def get_one_connected_narratives_and_outlier_narratives(selectedData_wo_Dates_naratives,target,connected_dimns_df):
    connected_dims_narratives_all=pd.DataFrame()
    one_connected_dims_outliers_narratives_all=pd.DataFrame()
    connected_dimns = connected_dimns_df[connected_dimns_df['connected_dimn_1'].notna()]
    
    #connected_dimns.to_excel('connected_dimns.xlsx')
    for key_dimn,column_values in connected_dimns.iterrows():
        try:
            # key_dimn='Priority'
            # column_values['connected_dimn_1']='myW Priority'
            
            grouped_conn_df = selectedData_wo_Dates_naratives.groupby([key_dimn, column_values['connected_dimn_1']])
            grouped_counter =grouped_conn_df.size().reset_index(name='counts')
            grouped_summed_conn_df=grouped_conn_df[target].sum().reset_index()        
            grouped_summed_conn_df['counts']=grouped_counter['counts']
            total_target_mean=selectedData_wo_Dates_naratives[target].mean()
            grouped_summed_conn_df["average"]=grouped_summed_conn_df[target]/grouped_summed_conn_df['counts']
            target_per=((grouped_summed_conn_df['average']-total_target_mean)/total_target_mean)*100
            grouped_summed_conn_df["percentage"]=target_per
            #grouped_summed_conn_df.sort_values(by=['percentage'],inplace=True, ascending=False)
            
            #generating narratives for Outliers 
            overall_sd=std_dev(selectedData_wo_Dates_naratives[target])  #std_dev  function in outlier section
            narratives_series=outlier_function_one_connected(grouped_summed_conn_df,total_target_mean,overall_sd)
            if narratives_series.empty==False:
                for index,rows in narratives_series.iterrows(): 
                        #print(index,rows.index[0])
                       one_connected_dims_outliers_narratives_all= one_connected_dims_outliers_narratives_all.append({
                                'dim1':rows.index[0],'dim1_value':str(rows[0]),
                                'dim2':rows.index[1],'dim2_value':str(rows[1]),                            
                                'metric':rows.index[2],'metric_value':rows[4],
                                'target_zscore':rows['target_zscore']},ignore_index=True)            
                
            
            
            #appending narratives to dataframe
            for index, row in  grouped_summed_conn_df.iterrows():
                #print(row[3])
                connected_dims_narratives_all = connected_dims_narratives_all.append(
                    {'dim1':row.index[0],'dim1_value':str(row[0]),
                     'dim2':row.index[1],'dim2_value':str(row[1]),
                     'metric':row.index[2],'metric_value':row[4],
                     'percentage':row[5]} ,ignore_index=True)
        except KeyError as e:
            error_key=e.args
    final_grouped_df=top5_bottom_5(connected_dims_narratives_all)

    #generating narratives for connected dimensions
    final_grouped_df = final_grouped_df.apply(lambda row: CreateMetricDimensionNarrative_one_conn(row), axis=1)
    
    if len(one_connected_dims_outliers_narratives_all)==0: #check if there  are no outliers then no of records  is zero
        outlier_narratives=None
    else: 
        final_outliers=top5_bottom_5_outliers(one_connected_dims_outliers_narratives_all)
        final_outliers_df=final_outliers.apply(lambda row: CreateOutlierNarrative_one_connected(row), axis=1)
        outlier_narratives=pd.DataFrame(list(final_outliers_df))

    # connected_dims_narratives_all.to_excel("connected_dims_narratives_all.xlsx")
    # outlier_narratives.to_excel("one_connected_dims_outliers_narratives_all.xlsx")
    return pd.DataFrame(list(final_grouped_df)),outlier_narratives
    





# =============================================================================
# if metric is given by the user  is catageroical
# 
# =============================================================================

def get_important_dim_by_classifier(df,cat_target):
    #convert object type columns so that we can get correlation
    selectedData_wo_Dates_categorical=df.copy()
    for col in selectedData_wo_Dates_categorical.columns:
        
        if str(selectedData_wo_Dates_categorical[col].dtype) in ['object','bool']:
        #encoding categorical columns
            try:
                selectedData_wo_Dates_categorical[col] = selectedData_wo_Dates_categorical[col].astype('category').cat.codes
            except ValueError:
                error_encounterd = "ValueError"
        # else:
        #             #removing contionus columns if there is any
        #     selectedData_wo_Dates_categorical.drop(col ,inplace=True,axis=1)


    #selectedData_wo_Dates_categorical.to_excel("selectedData_wo_Dates_categorical_encoded_values.xlsx")

    all_columns_wo_target=list(selectedData_wo_Dates_categorical.columns)
    all_columns_wo_target.remove(cat_target)
    
    imp_columns_with_classifier = pd.DataFrame(columns=['Feature','score'])

    X_cat=selectedData_wo_Dates_categorical[all_columns_wo_target]
    y_cat=selectedData_wo_Dates_categorical[cat_target]

    model_cat = RandomForestClassifier()
    # fit the model
    model_cat.fit(X_cat, y_cat)
    # get importance
    importance_cat = model_cat.feature_importances_
    # summarize feature importance
    for i,v in enumerate(importance_cat):
        imp_columns_with_classifier.loc[len(imp_columns_with_classifier.index)] = [X_cat.columns[i],v] 

    imp_columns_with_classifier = imp_columns_with_classifier.sort_values(by=['score'],ascending=False)
    

    imp_columns_with_classifier.reset_index(drop=True,inplace=True)

    all_list=list(imp_columns_with_classifier['Feature'])
    top15_dim_list_classifier = filter_impfeatures(X_cat, all_list)
    

    
    return top15_dim_list_classifier,all_columns_wo_target


# =============================================================================
# #generating naratives  (target is categorical)
# 
# =============================================================================


################################ two connected (level3) ######################################
# selectedData_wo_Dates_naratives=selectData_wo_dates_for_cat_metric.copy()
# cat_target_imp_dimns=imp_dims

def Create_Cat_MetricDimensionNarrative_two_conn(row):
    col0=row.index[0]#dim1
    col1=row.index[1]#dim1_value
    col2=row.index[2]#dim2
    col3=row.index[3]#dim2_value
    col4=row.index[4]#dim3
    col5=row.index[5]#dim3_value
    col6=row.index[6]#metric
    col7=row.index[7]#metric_prob
    col8=row.index[8]#metric_prob_error    
    col9=row.index[9]#metric_value    
    col10=row.index[10]#percentage
    row[col10]=round(row[col10],2)
    if row[col10] >=0:
        percentage=row[col10]
        x_times=round(percentage/100,2)+1
        type_temp='increase'
        
        narrative= "The probability of '"+str(row[col6]) + "' being '"+str(row[col9])+"' is "+str(row[col10])+" % more when '" + \
                str(row[col0]) +"' is '"+str(row[col1])+"' , '"+str(row[col2]) +"' is '"+str(row[col3])+"' and '" +\
                  str(row[col4]) +"' is '"+str(row[col5])+"'"
                  
        narrative1= "The probability of '"+str(row[col6]) + "' being '"+str(row[col9])+"' is "+str(x_times)+"X times  when '" + \
                str(row[col0]) +"' is '"+str(row[col1])+"' , '"+str(row[col2]) +"' is '"+str(row[col3])+"' and '" +\
                  str(row[col4]) +"' is '"+str(row[col5])+"'"          
                  
    else:
        percentage=-1*row[col10]
        x_times=round(-1*percentage/100,2)+1
        type_temp='decrease'
        narrative= "The probability of '"+str(row[col6]) + "' being '"+str(row[col9])+"' is "+str(-1*row[col10])+" % less when '" + \
                str(row[col0]) +"' is '"+str(row[col1])+"' , '"+str(row[col2]) +"' is '"+str(row[col3])+"' and '" +\
                  str(row[col4]) +"' is '"+str(row[col5])+"'"
                  
        narrative1= "The probability of '"+str(row[col6]) + "' being '"+str(row[col9])+"' is "+str(x_times)+"X times  when '" + \
                str(row[col0]) +"' is '"+str(row[col1])+"' , '"+str(row[col2]) +"' is '"+str(row[col3])+"' and '" +\
                  str(row[col4]) +"' is '"+str(row[col5])+"'"
    
    
    narrative_dict={'dim1':row[col0],"dim1_value":row[col1],
                    'dim2':row[col2],"dim2_value":row[col3],
                    'dim3':row[col4],"dim3_value":row[col5],                    
                    'metric':row[col6],'metric_value':row['metric_value'],'percentage':percentage,'X_times':x_times,'type':type_temp,'narratives':narrative} 
    return narrative_dict
                  
# selectedData_wo_Dates_naratives= selectData_wo_dates_for_cat_metric.copy()
# cat_target_connected_dimns_df_classifier=connected_dim_list_classifier_cc.copy()

def get_two_connected_narratives_with_cat_target_outlier_narratives(selectedData_wo_Dates_naratives,cat_target,cat_target_connected_dimns_df_classifier):
    
    cat_target_two_connected_dims_narratives_all=pd.DataFrame()
    outliers_narratives_all=pd.DataFrame()
    cat_target_connected_dimns_df_classifier = cat_target_connected_dimns_df_classifier.dropna()

    for key_dimn,column_values in cat_target_connected_dimns_df_classifier.iterrows():
        try:
            cat_grouped_conn_df = selectedData_wo_Dates_naratives.groupby([cat_target,key_dimn, column_values['connected_dimn_1'],column_values['connected_dimn_2']]).size().reset_index(name="counts")
            cat_summed_counts_df=cat_grouped_conn_df.groupby([cat_target]).sum().reset_index()  #total value counts in each class
            #cat_avg_counts_df=cat_grouped_conn_df.groupby([cat_target]).mean().reset_index()
            values_df=cat_grouped_conn_df[cat_target].value_counts().reset_index(name='groups').rename({'index':cat_target},axis=1)
            
            for key1,cvalues1 in cat_summed_counts_df.iterrows():  # cvalues1 has total count
                classes=int(values_df[values_df[cat_target]==cvalues1[cat_target]]['groups'])
                avg_prob=1/classes
                #print(classes,avg_prob)
                temp_grp_df=pd.DataFrame()
                for key2,cvalues2 in cat_grouped_conn_df.iterrows():
                    if cvalues2[cat_target]==cvalues1[cat_target]:
                        cat_grouped_conn_df.loc[key2,'probability']=cvalues2['counts']/cvalues1['counts']
                        cat_grouped_conn_df.loc[key2,'probability_error']=((cvalues2['counts']/cvalues1['counts'])-avg_prob)/avg_prob  ##probability error=(experminetal_prob-theoritical_prob)theoritical_prob
                        temp_grp_df=temp_grp_df.append(
                            {'metric':cat_target,'metric_value':cvalues2[cat_target],
                             'dim1':cvalues2.index[1],'dim1_value':str(cvalues2[1]),
                             'dim2':cvalues2.index[2],'dim2_value':str(cvalues2[2]),
                             'dim3':cvalues2.index[3],'dim3_value':str(cvalues2[3]),'counts':cvalues2['counts']},ignore_index=True)
                if len(temp_grp_df['counts']) >1:
                    grp_stddev=std_dev(temp_grp_df['counts'])  #std_dev  function in outlier section
                    grp_mean = temp_grp_df['counts'].mean()
                    if grp_stddev>0:
                        narratives_series=outlier_function_cat_target_two_conn(temp_grp_df,cat_target,grp_mean,grp_stddev)
                        if narratives_series.empty==False: 
                            for index,rows in narratives_series.iterrows(): 
                               outliers_narratives_all= outliers_narratives_all.append({
                                        'counts':rows['counts'],'dim1':rows['dim1'],'dim1_value':str(rows['dim1_value']),
                                        'dim2':rows['dim2'],'dim2_value':str(rows['dim2_value']),
                                        'dim3':rows['dim3'],'dim3_value':str(rows['dim3_value']),
                                        'metric':rows['metric'],'metric_value':rows['metric_value'],
                                        'target_zscore':rows['target_zscore']},ignore_index=True)            
                    
            cat_grouped_conn_df['percentage']=cat_grouped_conn_df['probability_error']*100
            
            for index, row in  cat_grouped_conn_df.iterrows():
                cat_target_two_connected_dims_narratives_all = cat_target_two_connected_dims_narratives_all.append(
                    {'dim1':row.index[1],'dim1_value':str(row[1]),
                     'dim2':row.index[2],'dim2_value':str(row[2]),
                     'dim3':row.index[3],'dim3_value':str(row[3]),
                     'metric':row.index[0],'metric_value':row[0],'metric_probability':row[5],'metric_probability_error':row[6],
                     'percentage':row[7]} ,ignore_index=True)
        except KeyError as e:
            error_key=e.args
    final_grouped_df=top5_bottom_5(cat_target_two_connected_dims_narratives_all)
    
    final_grouped_df = final_grouped_df.apply(lambda row: Create_Cat_MetricDimensionNarrative_two_conn(row), axis=1)
    if len(outliers_narratives_all)==0: #check if there  are no outliers then no of records  is zero
        outlier_narratives=None
    else: 
        final_outliers=top5_bottom_5_outliers(outliers_narratives_all)
        final_outliers_all=final_outliers.apply(lambda row:Create_Outlier_Narrative_cat_target_two_conn(row),axis=1)
        outlier_narratives=pd.DataFrame(list(final_outliers_all))
    #cat_target_two_connected_dims_narratives_all.to_excel('cat_target_two_connected_dims_narratives_all.xlsx')
    return pd.DataFrame(list(final_grouped_df)),outlier_narratives
# df=temp_grp_df.copy()
# popmean=grp_mean
# popsd=grp_stddev
def outlier_function_cat_target_two_conn(df,cat_target,popmean,popsd):
    
    temp_df=df.copy()

    temp_df['target_zscore']=z_score_calculate(temp_df['counts'],popmean,popsd)
    #temp_df.to_excel('temp_df.xlsx')

    for index,columnrows in temp_df.iterrows():
        if columnrows['target_zscore'] <3 and columnrows['target_zscore'] >-3:
            temp_df.drop(index,inplace=True,axis=0)
    if temp_df.empty==True:
        return pd.Series()
    else:
        return temp_df 
    
def Create_Outlier_Narrative_cat_target_two_conn(row):
    col0=row['counts'] 
    col1=row['dim1']
    col2=row['dim1_value'] 
    col3=row['dim2']
    col4=row['dim2_value'] 
    col5=row['dim3'] 
    col6=row['dim3_value'] 
    col7=row['metric'] 
    col8=row['metric_value'] 
    col9=row['target_zscore'] 
    
    if col9 >0:
        narrative ="When '"+ str(col1) +"' is '"+str(col2) +"' , '"+str(col3) +"' is '"+str(col4) +"' and '"+str(col5) +"' is '"+str(col6) +"' unusual number of times (very often) '"+ str(col7) + "' is '" + str(col8) + "'"
    else:
        narrative ="When '"+ str(col1) +"' is '"+str(col2) +"' , '"+str(col3) +"' is '"+str(col4) +"' and '"+str(col5) +"' is '"+str(col6) +"' unusual number of times (very few) '"+ str(col7) + "' is '" + str(col8) + "'"
    
    
    #narrative = "Unusual "+ str(col7) + " being '" + str(col8) + "' is observed when "+ str(col1) +" = '"+str(col2) +"' ,"+str(col3) +" = '"+str(col4) +"' and "+str(col5) +" = '"+str(col6) +"'"
                
    outlier_narr={
            'metric':col7,'metric_value':col8,'type':'Unusual','dim1':col1,'dim1_value':col2,'dim2':col3,'dim2_value':col4,'dim3':col5,'dim3_value':col6,
            'narratives':narrative
            } 
    
    return outlier_narr


############################## end for two connected ##############################
 
############################ one connected  (level 2) ############################

def Create_Cat_MetricDimensionNarrative_one_conn(row):
    col0=row.index[0]#dim1
    col1=row.index[1]#dim1_value
    col2=row.index[2]#dim2
    col3=row.index[3]#dim2_value
    col4=row.index[4]#metric
    col5=row.index[5]#metric_prob
    col6=row.index[6]#metric_prob_error    
    col7=row.index[7]#metric_value    
    col8=row.index[8]#percentage
    row[col8]=round(row[col8],2)
    if row[col8] >=0:
        percentage=row[col8]
        x_times=round(percentage/100,2)+1
        type_temp='increase'
        narrative= "The probability of '"+str(row[col4]) + "' being '"+str(row[col7])+"' is "+str(row[col8])+" % more when '" + \
                str(row[col0]) +"' is '"+str(row[col1])+"'  and '" +\
                  str(row[col2]) +"' is '"+str(row[col3])+"'"
        narrative1= "The probability of '"+str(row[col4]) + "' being '"+str(row[col7])+"' is "+str(x_times)+"X times  when '" + \
                str(row[col0]) +"' is '"+str(row[col1])+"'  and '" +\
                  str(row[col2]) +"' is '"+str(row[col3])+"'"
        
    else:
        percentage=-1*row[col8]
        x_times=round(-1*percentage/100,2)+1
        type_temp='decrease'
        narrative= "The probability of '"+str(row[col4]) + "' being '"+str(row[col7])+"' is "+str(-1*row[col8])+"% less when '" + \
                str(row[col0]) +"' is '"+str(row[col1])+"'  and '" +\
                  str(row[col2]) +"' is '"+str(row[col3])+"'"
        narrative1= "The probability of '"+str(row[col4]) + "' being '"+str(row[col7])+"' is "+str(x_times)+"X times  when '" + \
                str(row[col0]) +"' is '"+str(row[col1])+"'  and '" +\
                  str(row[col2]) +"' is '"+str(row[col3])+"'"
    
    narrative_dict={'dim1':row[col0],"dim1_value":row[col1],
                    'dim2':row[col2],"dim2_value":row[col3],                    
                    'metric':row[col4],'metric_value':row['metric_value'],'percentage':percentage,'X_times':x_times,'type':type_temp,'narratives':narrative} 
    return narrative_dict

# selectedData_wo_Dates_naratives=selectData_for_cat_metric_narrative.copy()
# cat_target_connected_dimns_df_classifier=connected_dim_list_classifier_cc.copy()

def get_one_connected_narratives_with_cat_target_outlier_narratives(selectedData_wo_Dates_naratives,cat_target,cat_target_connected_dimns_df_classifier):
    
    cat_target_one_connected_dims_narratives_all=pd.DataFrame()
    outliers_narratives_all=pd.DataFrame()
    cat_target_connected_dimns_df_classifier = cat_target_connected_dimns_df_classifier[cat_target_connected_dimns_df_classifier['connected_dimn_1'].notna()]

    
    for key_dimn,column_values in cat_target_connected_dimns_df_classifier.iterrows():
        try:
            #print(key_dimn,"---",column_values)
            cat_grouped_conn_df = selectedData_wo_Dates_naratives.groupby([cat_target,key_dimn, column_values['connected_dimn_1']]).size().reset_index(name="counts")
            cat_summed_counts_df=cat_grouped_conn_df.groupby([cat_target]).sum().reset_index()  #total value counts in each class
            values_df=cat_grouped_conn_df[cat_target].value_counts().reset_index(name='counts').rename({'index':cat_target},axis=1)
    
            for key1,cvalues1 in cat_summed_counts_df.iterrows():  # cvalues1 has total count
                #print(key1,cvalues1)
                classes=int(values_df[values_df[cat_target]==cvalues1[cat_target]]['counts'])
                avg_prob=1/classes    #total prob/no of classes    
                temp_grp_df=pd.DataFrame()
    
                for key2,cvalues2 in cat_grouped_conn_df.iterrows():
                    #print(key2,cvalues2)
                    if cvalues2[cat_target]==cvalues1[cat_target]:
                        cat_grouped_conn_df.loc[key2,'probability']=cvalues2['counts']/cvalues1['counts']
                        cat_grouped_conn_df.loc[key2,'probability_error']=((cvalues2['counts']/cvalues1['counts'])-avg_prob)/avg_prob  ##probability error=(experminetal_prob-theoritical_prob)theoritical_prob
                        temp_grp_df=temp_grp_df.append(
                            {'metric':cat_target,'metric_value':cvalues2[cat_target],
                             'dim1':cvalues2.index[1],'dim1_value':str(cvalues2[1]),
                             'dim2':cvalues2.index[2],'dim2_value':str(cvalues2[2]),
                             'counts':cvalues2['counts']},ignore_index=True)
                if len(temp_grp_df['counts']) >1:
                    grp_stddev=std_dev(temp_grp_df['counts'])  #std_dev  function in outlier section
                    grp_mean = temp_grp_df['counts'].mean()
                    if grp_stddev>0:
                        
                        narratives_series=outlier_function_cat_target_one_conn(temp_grp_df,cat_target,grp_mean,grp_stddev)
                        if narratives_series.empty==False: 
                            for index,rows in narratives_series.iterrows(): 
                               outliers_narratives_all= outliers_narratives_all.append({
                                        'counts':rows['counts'],'dim1':rows['dim1'],'dim1_value':str(rows['dim1_value']),
                                        'dim2':rows['dim2'],'dim2_value':str(rows['dim2_value']),
                                        'metric':rows['metric'],'metric_value':rows['metric_value'],
                                        'target_zscore':rows['target_zscore']},ignore_index=True)            
                
            cat_grouped_conn_df['percentage']=cat_grouped_conn_df['probability_error']*100
            # cat_grouped_conn_df.sort_values(by=['percentage'],inplace=True, ascending=False)
            # for index, colrow in cat_grouped_conn_df.iterrows():
            
            #     cat_grouped_conn_df['Narrative'] = cat_grouped_conn_df.apply(lambda row: Create_Cat_MetricDimensionNarrative_one_conn(row,colrow), axis=1)
        
            for index, row in  cat_grouped_conn_df.iterrows():
                #print(row['Narrative'])
                cat_target_one_connected_dims_narratives_all = cat_target_one_connected_dims_narratives_all.append(
                    {'dim1':row.index[1],'dim1_value':str(row[1]),
                     'dim2':row.index[2],'dim2_value':str(row[2]),
                     'metric':row.index[0],'metric_value':row[0],'metric_probability':row[4],'metric_probability_error':row[5],
                     'percentage':row[6]} ,ignore_index=True)
        except KeyError as e:
            error_key=e.args
    final_grouped_df=top5_bottom_5(cat_target_one_connected_dims_narratives_all)
    final_grouped_df = final_grouped_df.apply(lambda row: Create_Cat_MetricDimensionNarrative_one_conn(row), axis=1)
    if len(outliers_narratives_all)==0: #check if there  are no outliers then no of records  is zero
        outlier_narratives=None
    else:
        final_outliers=top5_bottom_5_outliers(outliers_narratives_all)
        final_outliers_all=final_outliers.apply(lambda row:Create_Outlier_Narrative_cat_target_one_conn(row),axis=1)
        outlier_narratives=pd.DataFrame(list(final_outliers_all))
    #cat_target_two_connected_dims_narratives_all.to_excel('cat_target_two_connected_dims_narratives_all.xlsx')
    return pd.DataFrame(list(final_grouped_df)),outlier_narratives



# df=temp_grp_df.copy()
# popmean=grp_mean
# popsd=grp_stddev
def outlier_function_cat_target_one_conn(df,cat_target,popmean,popsd):
    
    temp_df=df.copy()

    temp_df['target_zscore']=z_score_calculate(temp_df['counts'],popmean,popsd)
    #temp_df.to_excel('temp_df.xlsx')

    for index,columnrows in temp_df.iterrows():
        if columnrows['target_zscore'] <3 and columnrows['target_zscore'] >-3:
            temp_df.drop(index,inplace=True,axis=0)
    if temp_df.empty==True:
        return pd.Series()
    else:
        return temp_df
    
def Create_Outlier_Narrative_cat_target_one_conn(row):
    col0=row['counts'] 
    col1=row['dim1'] 
    col2=row['dim1_value'] 
    col3=row['dim2'] 
    col4=row['dim2_value'] 
    col5=row['metric'] 
    col6=row['metric_value']
    col7=row['target_zscore'] 

    
    if col7 >0:
        narrative ="When '"+ str(col1) +"' is '"+str(col2) +"'  and '"+str(col3) +"' is '"+str(col4) +"' unusual number of times (very often) '"+ str(col5) + "' is '" + str(col6) + "'"
    else:
        narrative ="When '"+ str(col1) +"' is '"+str(col2) +"'  and '"+str(col3) +"' is '"+str(col4) +"' unusual number of times (very few) '"+ str(col5) + "' is '" + str(col6) + "'"
    
    
    #narrative = "Unusual "+ str(col5) + " being '" + str(col6) + "' is observed when "+ str(col1) +" = '"+str(col2) +"' and "+str(col3) +" = '"+str(col4) +"'"
                
    outlier_narr={
            'metric':col5,'metric_value':col6,'type':'Unusual','dim1':col1,'dim1_value':col2,'dim2':col3,'dim2_value':col4,
            'narratives':narrative
            } 
    
    return outlier_narr

############################# end for one connected ########################

############################# imp dims narratives (level 1)##########################

def Create_Cat_MetricDimensionNarrative(row):
    col0=row.index[0]#dim1
    col1=row.index[1]#dim1_value
    col2=row.index[2]#metric
    col3=row.index[3]#metric_prob
    col4=row.index[4]#metric_prob_error  
    col5=row.index[5]#metric_value  
    col6=row.index[6]#percentage
    row[col6]=round(row[col6],2)
    if row[col6] >=0:
        type_temp='increase'
        percentage=row[col6]
        x_times=round(percentage/100,2)+1

        narrative= "The probability of '"+str(row[col2]) + "' being '"+str(row[col5])+"' is "+str(row[col6])+"% more when '" + \
                str(row[col0]) +"' is '"+str(row[col1])+"'" 
        narrative1= "The probability of '"+str(row[col2]) + "' being '"+str(row[col5])+"' is "+str(x_times)+"X times  when '" + \
                str(row[col0]) +"' is '"+str(row[col1])+"'"
    else:
        type_temp='decrease'
        percentage=-1*row[col6]
        x_times=round(-1*percentage/100,2)+1
        
        narrative= "The probability of '"+str(row[col2]) + "' being '"+str(row[col5])+"' is "+str(-1*row[col6])+" % less when '" + \
                str(row[col0]) +"' is '"+str(row[col1])+"'"
                
        narrative1= "The probability of '"+str(row[col2]) + "' being '"+str(row[col5])+"' is "+str(x_times)+"X times  when '" + \
                str(row[col0]) +"' is '"+str(row[col1])+"'" 
        
    narrative_dict={'dim1':row[col0],"dim1_value":row[col1],
                    'metric':row[col2],'metric_value':row['metric_value'],'percentage':percentage,'X_times':x_times,'type':type_temp,'narratives':narrative} 
    return narrative_dict

"""
cat_target= targetfornarrative
cat_target_imp_dimns= imp_features
selectedData_wo_Dates_naratives=selectData_for_cat_metric_narrative.copy()
"""
def get_imp_dim_narratives_with_cat_target_outlier_narratives(selectedData_wo_Dates_naratives,cat_target,cat_target_imp_dimns):
    
    cat_target_imp_dims_narratives_all=pd.DataFrame()
    outliers_narratives_all=pd.DataFrame()
    for key_dimn in cat_target_imp_dimns:
        try:
            cat_grouped_conn_df = selectedData_wo_Dates_naratives.groupby([cat_target,key_dimn]).size().reset_index(name="counts")
            cat_summed_counts_df=cat_grouped_conn_df.groupby([cat_target]).sum().reset_index()  #total value counts in each class
            values_df=cat_grouped_conn_df[cat_target].value_counts().reset_index(name='counts').rename({'index':cat_target},axis=1)
    
            for key1,cvalues1 in cat_summed_counts_df.iterrows():  # cvalues1 has total count
                classes=int(values_df[values_df[cat_target]==cvalues1[cat_target]]['counts'])
                avg_prob=1/classes
                temp_grp_df=pd.DataFrame()
    
                #print(classes,avg_prob)
                for key2,cvalues2 in cat_grouped_conn_df.iterrows():
                    
                    if cvalues2[cat_target]==cvalues1[cat_target]:
                        cat_grouped_conn_df.loc[key2,'probability']=cvalues2['counts']/cvalues1['counts']
                        cat_grouped_conn_df.loc[key2,'probability_error']=((cvalues2['counts']/cvalues1['counts'])-avg_prob)/avg_prob  ##probability error=(experminetal_prob-theoritical_prob)theoritical_prob
                        temp_grp_df=temp_grp_df.append(
                            {'metric':cat_target,'metric_value':cvalues2[cat_target],
                             'dim1':cvalues2.index[1],'dim1_value':str(cvalues2[1]),
                             'counts':cvalues2['counts']},ignore_index=True)
                if len(temp_grp_df['counts']) >1:
                    
                    grp_stddev=std_dev(temp_grp_df['counts'])  #std_dev  function in outlier section
                    #temp_stddev=stdev(temp_grp_df['counts'])
                    #print('std_dev1::',grp_stddev,"std_dev2::",temp_stddev)
                    grp_mean = temp_grp_df['counts'].mean()
                    if grp_stddev>0:
                        
                        #print(key2)
                        narratives_series=outlier_function_cat_target(temp_grp_df,cat_target,grp_mean,grp_stddev)
                        #print(narratives_series)
                        if narratives_series.empty==False:
                            #print(narratives_series)
                            for index,rows in narratives_series.iterrows(): 
                               outliers_narratives_all= outliers_narratives_all.append({
                                        'counts':rows['counts'],'dim1':rows['dim1'],'dim1_value':str(rows['dim1_value']),
                                        'metric':rows['metric'],'metric_value':rows['metric_value'],
                                        'target_zscore':rows['target_zscore']},ignore_index=True)            
            
            
    
            cat_grouped_conn_df['percentage']=cat_grouped_conn_df['probability_error']*100
            # cat_grouped_conn_df.sort_values(by=['percentage'],inplace=True, ascending=False)
            # for index, colrow in cat_grouped_conn_df.iterrows():
            
            #     cat_grouped_conn_df['Narrative'] = cat_grouped_conn_df.apply(lambda row: Create_Cat_MetricDimensionNarrative_one_conn(row,colrow), axis=1)
        
            for index, row in  cat_grouped_conn_df.iterrows():
                #print(row['Narrative'])
                cat_target_imp_dims_narratives_all = cat_target_imp_dims_narratives_all.append(
                    {'dim1':row.index[1],'dim1_value':str(row[1]),
                     'metric':row.index[0],'metric_value':row[0],'metric_probability':row[3],'metric_probability_error':row[4],
                     'percentage':row[5]} ,ignore_index=True)
        except KeyError as e:
            error_key=e.args
    final_grouped_df=top5_bottom_5(cat_target_imp_dims_narratives_all)
    final_grouped_df= final_grouped_df.apply(lambda row: Create_Cat_MetricDimensionNarrative(row), axis=1)
    if len(outliers_narratives_all)==0: #check if there  are no outliers then no of records  is zero
        outlier_narratives=None
    else:
        final_outliers=top5_bottom_5_outliers(outliers_narratives_all)
        final_outliers_all=final_outliers.apply(lambda row:Create_Outlier_Narrative_cat_target(row),axis=1)
        outlier_narratives=pd.DataFrame(list(final_outliers_all))
    #cat_target_two_connected_dims_narratives_all.to_excel('cat_target_two_connected_dims_narratives_all.xlsx')
    return pd.DataFrame(list(final_grouped_df)),outlier_narratives

# df=temp_grp_df.copy()
# popmean=grp_mean
# popsd=grp_stddev
def outlier_function_cat_target(df,cat_target,popmean,popsd):
    
    temp_df=df.copy()

    temp_df['target_zscore']=z_score_calculate(temp_df['counts'],popmean,popsd)
    #temp_df.to_excel('temp_df.xlsx')

    for index,columnrows in temp_df.iterrows():
        if columnrows['target_zscore'] <3 and columnrows['target_zscore'] >-3:
            temp_df.drop(index,inplace=True,axis=0)
    if temp_df.empty==True:
        return pd.DataFrame()
    else:
        # temp_df_final=top5_bottom_5_outliers(temp_df)
        #outlier_narr_temp = temp_df.apply(lambda row: Create_Outlier_Narrative_cat_target(row), axis=1) 
        
        outlier_narr_temp=temp_df
        return outlier_narr_temp 
    
def Create_Outlier_Narrative_cat_target(row):
    col0=row['counts'] 
    col1=row['dim1'] 
    col2=row['dim1_value'] 
    col3=row['metric'] 
    col4=row['metric_value'] 
    col5=row['target_zscore']
    
    if col5 >0:
        narrative ="When '"+ str(col1) +"' is '"+str(col2) +"' unusual number of times (very often) '"+ str(col3) + "' is '" + str(col4) + "'"
    else:
        narrative ="When '"+ str(col1) +"' is '"+str(col2) +"' unusual number of times (very few) '"+ str(col3) + "' is '" + str(col4) + "'"
        
    
    #narrative = "Unusual "+ str(col3) + " being '" + str(col4) + "' is observed when "+ str(col1) +" = '"+str(col2) +"'"
                
    outlier_narr={
            'metric':col3,'metric_value':col4,'type':'Unusual','dim1':col1,'dim1_value':col2,
            'narratives':narrative
            } 
    
    return outlier_narr




############################## end for imp dims narratives #################


# =============================================================================
#end for generating naratives  (target is categorical)
# =============================================================================

  


# =============================================================================
# 
# trend analysis ::  by pinniti.kumar.reddy
#
#   
# =============================================================================
#df = date_fortrend.copy()
def get_day_df(df,date_by_user):
    from datetime import datetime
    df_day=df.copy()
    # df_day = pd.to_datetime(df_day)
    # dt = datetime.now()
    # day_df=pd.DataFrame()
    # day_df[date_by_user] = [dt.date() for dt in df_day]
    
                                                                                     
    
    day_df = pd.DataFrame(pd.to_datetime(df_day,dayfirst=True))
    day_df[date_by_user]=day_df[date_by_user].dt.strftime('%d-%m-%Y') #these steps are repeated to remove  hours in dateformat
    day_df = pd.DataFrame(pd.to_datetime(day_df[date_by_user],dayfirst=True))

    
    date_counter = day_df.groupby([date_by_user]).size().reset_index(name = 'counts')
    
    week_counter = date_counter.groupby(pd.Grouper(key=date_by_user,freq='1W')).sum()
    
   

    #date_counter.rename(columns={date_counter.columns[0]:'Index'},inplace=True)
    
    date_counter.set_index(date_counter[date_by_user],inplace=True)
    date_counter.drop(columns=[date_by_user],inplace=True)
        
    idx = pd.date_range(start=date_counter.index[0], end=date_counter.index[-1])
    
    date_counter.index = pd.DatetimeIndex(date_counter.index)
    
    date_counter = date_counter.reindex(idx, fill_value=0)
    print("len(date_counter)"+str(len(date_counter)))
    for i in range (0,len(date_counter)):
        if int(date_counter.values[i]) == 0:
            try:
                if(i>2 and i<(len(date_counter)-3)):                
                    avge = (date_counter.values[i-3] + date_counter.values[i-2] + date_counter.values[i-1] + date_counter.values[i+1] + date_counter.values[i+2] + date_counter.values[i+3])
                    avge = avge / 6
                elif(i<=2):
                    avge = (date_counter.values[i+1] + date_counter.values[i+2] + date_counter.values[i+3])
                    avge = avge / 3
                elif(i>=(len(date_counter)-3)):
                    avge = (date_counter.values[i-3] + date_counter.values[i-2] + date_counter.values[i-1])
                    avge = avge / 3
                date_counter.iloc[i] = int(np.round(avge))
            except Exception as e:
                print(e.args)
    date_counter.reset_index(level=0, inplace=True)
    date_counter.rename(columns={"index":date_by_user},inplace=True)
    
    week_counter.reset_index(level=0, inplace=True)
    week_counter.rename(columns={"index":date_by_user},inplace=True)
    
    return date_counter,week_counter

# week_counter = week_df.copy()
def get_Week_Prediction(week_counter,entity,month_list):
    
    model = ARIMA(week_counter[week_counter.columns[-1]], order=(2, 1, 1))
    #try except is implemented ,issue is occured in FDS EU env :data specific issue
    try:
        model_fit = model.fit()
    except:
        model.initialize_approx
        model_fit = model.fit()
    
    prediction = pd.DataFrame()
    
    pred_week = pd.DataFrame()
    
    import datetime
    
    end = week_counter[week_counter.columns[0]].iloc[-1] + datetime.timedelta(days=80)
    
    week_index = pd.date_range(start = week_counter[week_counter.columns[0]].iloc[-1], end = end,freq = '1W')
    
    
    for i in range(0,10):
        yhat = model_fit.predict(len(week_counter), len(week_counter))
        week = {week_counter.columns[0]:week_index[i+1],week_counter.columns[-1]:yhat.iloc[0]}
        week['counts'] = int(week['counts'])
        
        if week['counts']<0:
            week['counts'] = 0

        week_counter = week_counter.append(pd.DataFrame(week,index = [len(week_counter)]))
        pred_week = pred_week.append(pd.DataFrame(week,index = [i]))
        
    
    # prediction = prediction.append(pred_date)
    #pred_count.to_excel(r"C:\Users\pinninti.kumar.reddy\Desktop\Prediction_Date_Pranay_Reddy.xlsx")
    pred_week[week_counter.columns[0]] = pred_week[week_counter.columns[0]].dt.strftime('%Y-%m-%d')
    pred_week_df=pred_week.apply(lambda row:CreateWeekPredictionNarrative(row,entity,month_list),axis=1)
    return pd.DataFrame(list(pred_week_df))


    

    #date

def get_Date_Prediction(date_counter,entity,month_list):
    
    model = ARIMA(date_counter[date_counter.columns[-1]], order=(2, 1, 1))
    model_fit = model.fit()
    
    prediction = pd.DataFrame()
    
    pred_date = pd.DataFrame()
    
    import datetime
    date = date_counter[date_counter.columns[0]].iloc[-1] + datetime.timedelta(days=1)
    # make prediction
    for i in range(0,10):
        yhat = model_fit.predict(len(date_counter), len(date_counter))
        data = {date_counter.columns[0]:date,date_counter.columns[-1]:yhat.iloc[0]}
        data['counts'] = int(data['counts'])
        
        if data['counts']<0:
           data['counts'] = 0

        date_counter = date_counter.append(pd.DataFrame(data,index = [len(date_counter)]))
        pred_date = pred_date.append(pd.DataFrame(data,index = [i]))
        date = date + datetime.timedelta(days=1)
    
    # prediction = prediction.append(pred_date)
    #pred_count.to_excel(r"C:\Users\pinninti.kumar.reddy\Desktop\Prediction_Date_Pranay_Reddy.xlsx")
    pred_date[date_counter.columns[0]] = pred_date[date_counter.columns[0]].dt.strftime('%Y-%m-%d')
    pred_date_df=pred_date.apply(lambda row:CreateDayPredictionNarrative(row,entity,month_list),axis=1)
    return pd.DataFrame(list(pred_date_df))

    
def get_Month_Prediction(month_counter,entity):
    
    #montly analyis 
    model = ARIMA(month_counter[month_counter.columns[-1]], order=(1, 1, 1))
    model_fit = model.fit()
    
    pred_month  = pd.DataFrame()
    
    for i in range(0,5):
        yhat = model_fit.predict(len(month_counter), len(month_counter), typ='levels')
        # yhat1 = model_fit.predict(len(month_counter), len(month_counter), typ='levels')
        # pred = pred + [yhat.iloc[0]]
        year = month_counter[month_counter.columns[0]].iloc[-1]
        month = month_counter[month_counter.columns[1]].iloc[-1]
        if month == 12 :
            year = year + 1
            month = 1
        else :
            month = month + 1
        data = {month_counter.columns[0]:year,month_counter.columns[1]:month,month_counter.columns[-1]:yhat.iloc[0]}
        data['counts'] = int(data['counts'])
        
        if data['counts']<0:
            data['counts'] = 0

        month_counter = month_counter.append(pd.DataFrame(data,index = [len(month_counter)]))
        pred_month = pred_month.append(pd.DataFrame(data,index = [i]))
        
    pred_month_df=pred_month.apply(lambda row:CreateMonthlyPredictionNarrative(row,entity),axis=1)    
    # prediction = prediction.append(pred_month)
    #pred_month.to_excel(r"C:\Users\pinninti.kumar.reddy\Desktop\Prediction_Of_Month_Pranay_Reddy.xlsx")
    
    return pd.DataFrame(list(pred_month_df))

def get_Quarter_Prediction(quarter_counter,entity):
    
  #quarter  
    model = ARIMA(quarter_counter[quarter_counter.columns[-1]], order=(1, 1, 1))
    model_fit = model.fit()
    
    
    pred_quarter  = pd.DataFrame()
    
    for i in range(0,5):
        # model = ARIMA(quarter_counter[quarter_counter.columns[-1]], order=(1, 1, 1))
        # model_fit = model.fit()
        yhat = model_fit.predict(len(quarter_counter), len(quarter_counter), typ='levels')
        #yhat1 = model_fit.predict(len(quarter_counter), len(quarter_counter), typ='levels')
        #pred = pred + [yhat.iloc[0]]
        year = quarter_counter[quarter_counter.columns[0]].iloc[-1]
        quarter = quarter_counter[quarter_counter.columns[1]].iloc[-1]
        if quarter == 4 :
            year = year + 1
            quarter = 1
        else :
            quarter = quarter + 1
        data = {quarter_counter.columns[0]:year,quarter_counter.columns[1]:quarter,quarter_counter.columns[-1]:yhat.iloc[0]}
        data['counts'] = int(data['counts'])
        
        if data['counts']<0:
            data['counts'] = 0

        quarter_counter = quarter_counter.append(pd.DataFrame(data,index = [len(quarter_counter)]))
        pred_quarter = pred_quarter.append(pd.DataFrame(data,index = [i]))
    
    pred_quarter_df=pred_quarter.apply(lambda row:CreateQuarterPredictionNarrative(row,entity),axis=1)    

    # prediction = prediction.append(pred_quarter)
    #pred_quarter.to_excel(r"C:\Users\pinninti.kumar.reddy\Desktop\Prediction_Of_Quarter_Pranay_Reddy.xlsx")
    
    return pd.DataFrame(list(pred_quarter_df))

def get_Year_Prediction(year_counter,entity):
    
#yearly
    model = ARIMA(year_counter[year_counter.columns[-1]], order=(1, 1, 1))
    model_fit = model.fit()
    
    pred_year  = pd.DataFrame()
    year = year_counter[year_counter.columns[0]].iloc[-1]
    for i in range(0,5):
        yhat = model_fit.predict(len(year_counter), len(year_counter), typ='levels')
        year = year + 1
        data = {year_counter.columns[0]:year,year_counter.columns[-1]:yhat.iloc[0]}
        data['counts'] = int(data['counts'])

        year_counter = year_counter.append(pd.DataFrame(data,index = [len(year_counter)]))
        pred_year = pred_year.append(pd.DataFrame(data,index = [i]))
        
    pred_year_df=pred_year.apply(lambda row:CreateYearlyPredictionNarrative(row,entity),axis=1)
    # prediction = prediction.append(pred_year)
    
    # prediction.to_excel(r"C:\Users\pinninti.kumar.reddy\Desktop\Files from Spyder\Prediction_Pranay_Reddy.xlsx")
        
    return pd.DataFrame(list(pred_year_df))

def CreateMonthlyPredictionNarrative(row,entity):
    narrative_df={'year':row[0],'month':calendar.month_name[int(row[1])],'prediction_value':row['counts']}
    #narrative="Count of "+entity+" is expected to be '"+str(row['counts'])+"' in '"+calendar.month_name[int(row[1])]+" "+str(int(row[0]))+"'"
    narrative_list=["Count of "+entity+" is expected to be '"+str(row['counts'])+"' in '"+calendar.month_name[int(row[1])]+" "+str(int(row[0]))+"'",
                    "Anticipated count of "+entity+" is '"+str(row['counts'])+"' on '"+calendar.month_name[int(row[1])]+" "+str(int(row[0]))+"'",
                    "Number of "+entity+" adding up on '"+calendar.month_name[int(row[1])]+" "+str(int(row[0]))+"' is expected to be '"+str(row['counts'])+"'"]
    
    narrative=random.choice(narrative_list)
    narrative_df['type']='prediction'
    narrative_df['narratives']=narrative
    return narrative_df

def CreateQuarterPredictionNarrative(row,entity):
    narrative_df={'year':row[0],'quarter':int(row[1]),'prediction_value':row['counts']}
    #narrative="Count of "+entity+" is expected to be '"+str(row['counts'])+"' for "+"'Quarter- "+str(int(row[1]))+" " + str(int(row[0]))+"'"
    narrative_list=["Count of "+entity+" is expected to be '"+str(row['counts'])+"' for "+"'Quarter- "+str(int(row[1]))+" " + str(int(row[0]))+"'",
                    "Anticipated count of "+entity+" is '"+str(row['counts'])+"' for "+"'Quarter- "+str(int(row[1]))+" " + str(int(row[0]))+"'",
                    "Number of "+entity+" adding up for "+"'Quarter- "+str(int(row[1]))+" " + str(int(row[0]))+"' is expected to be '"+str(row['counts'])+"'"]
    narrative=random.choice(narrative_list)
    narrative_df['type']='prediction'
    narrative_df['narratives']=narrative
    return narrative_df


def CreateDayPredictionNarrative(row,entity,month_list):    
    row_date = pd.to_datetime(row)
    date = str(row_date[0].day)+"-"+str(month_list[(row_date[0].month)-1])+"-"+str(row_date[0].year)
    #narrative="Count of "+entity+" is expected to be '"+str(row['counts'])+"' on '"+str(date)+"'"
    narrative_list=["Count of "+entity+" is expected to be '"+str(row['counts'])+"' on '"+str(date)+"'",
                    "Anticipated count of "+entity+" is '"+str(row['counts'])+"' on '"+str(date)+"'",
                    "Number of "+entity+" adding up on '"+str(date)+"' is expected to be '"+str(row['counts'])+"'"]
    narrative=random.choice(narrative_list)
    narrative_df={'day': date,'prediction_value':row['counts']}
    narrative="Count of "+entity+" is expected to be '"+str(row['counts'])+"' on '"+str(date)+"'"
    narrative_df['type']='prediction'
    narrative_df['narratives']=narrative
    return narrative_df


def CreateYearlyPredictionNarrative(row,entity):
    narrative_df={'day':row[0],'prediction_value':row['counts']}
    narrative="Analysing historical "+entity+" trend, expected to have '"+str(row['counts'])+"' of them for '"+str(int(row[0]))+"'"
    narrative_df['type']='prediction'
    narrative_df['narratives']=narrative
    return narrative_df


def CreateWeekPredictionNarrative(row,entity,month_list):    
    row_date = pd.to_datetime(row)
    date = str(row_date[0].day)+"-"+str(month_list[(row_date[0].month)-1])+"-"+str(row_date[0].year)
    narrative_df={'week':date,'prediction_value':row['counts']}
    #narrative="Count of "+entity+" is expected to be '"+str(row['counts'])+"' of them for 'week-ending "+date+"'"
    narrative_list=["Count of "+entity+" is expected to be '"+str(row['counts'])+"' of them for 'week-ending "+date+"'",
    "Number of "+entity+" adding up is anticipated to be '"+str(row['counts'])+"' for 'week-ending "+date+"'",
    "For 'week-ending "+date+"', number of "+entity+" calculated to be is '"+str(row['counts'])+"'"]
    narrative=random.choice(narrative_list)
    narrative_df['type']='prediction'
    narrative_df['narratives']=narrative
    return narrative_df




#####################################################################################

"""
cat_target= targetfornarrative
cat_target_imp_dimns= imp_features
selectedData_wo_Dates_naratives=selectData_for_cat_metric_narrative.copy()
cat_target_connected_dimns_df_classifier=connected_dims.copy()

"""

def get_imp_dim_narratives_with_cat_target(selectedData_wo_Dates_naratives,cat_target,cat_target_imp_dimns,entity):
    
    final_narratives=pd.DataFrame()
    for key_dimn in cat_target_imp_dimns:
        try:
            if key_dimn !=cat_target:
                cat_grouped_conn_df = selectedData_wo_Dates_naratives.groupby([cat_target,key_dimn]).size().reset_index(name="counts")    
                cat_summed_counts_df=cat_grouped_conn_df.groupby([cat_target]).sum().reset_index()  #total value counts in each class    for _,cvalues1 in values_df.iterrows():
                for _,cvalues1 in cat_summed_counts_df.iterrows():
                    #print(cvalues1)         
                    tempdf= cat_grouped_conn_df.loc[cat_grouped_conn_df[cat_target].isin([cvalues1[cat_target]])]
                    tempdf['percentage']=(tempdf['counts']/cvalues1['counts'])*100
                    q75, q25 = np.percentile(tempdf['percentage'], [75,25])
                    df_ltq25=tempdf.loc[tempdf['percentage']<q25]
                    if len(df_ltq25)>0:
                        #print('df_ltq25')
                        narrativedflt25q=pd.DataFrame()
                        for index,rows in df_ltq25.iterrows():
                          narrativedflt25q=narrativedflt25q.append({'metric':rows.index[0],'metric_value':rows[0],'dim1':rows.index[1],'dim1_value':rows[1],'counts':int(rows[2]),'total counts':int(cvalues1['counts']),'percentage':rows[3]  },ignore_index=True)  
                        narrativedflt25q['narratives']= narrativedflt25q.apply(lambda row: Create_Cat_MetricNarrativeLT25(row,entity), axis=1)
                        #list_narratives=list_narratives.append(narrativedflt25q)
                        #print(final_narratives['narratives'])
                        final_narratives=final_narratives.append(narrativedflt25q)
                    df_gtq75=tempdf.loc[tempdf['percentage']>=q75]
                    
                    if len(df_gtq75)>0:
                        narrativedfgt75q=pd.DataFrame()
                        for index,rows in df_gtq75.iterrows():
                          narrativedfgt75q=narrativedfgt75q.append({'metric':rows.index[0],'metric_value':rows[0],'dim1':rows.index[1],'dim1_value':rows[1],'counts':int(rows[2]),'total counts':int(cvalues1['counts']),'percentage':rows[3] },ignore_index=True)  
                        narrativedfgt75q['narratives']= narrativedfgt75q.apply(lambda row: Create_Cat_MetricNarrativeGT75(row,entity), axis=1)
                        #list_narratives=list_narratives.append(narrativedfgt75q)
                        final_narratives=final_narratives.append(narrativedfgt75q)
        except KeyError as e:
            error_key=e.args

    return final_narratives
                    


def get_connected_narratives_with_cat_target(selectedData_wo_Dates_naratives,cat_target,cat_target_connected_dimns_df_classifier,entity):
    level2final_narratives=pd.DataFrame()
    level3final_narratives=pd.DataFrame()
    
    cat_target_connected_dimns_df_classifier=cat_target_connected_dimns_df_classifier.dropna(how='all')
    for key_dimn,column_values in cat_target_connected_dimns_df_classifier.iterrows():
        try:
            cat_grouped_conn_df = selectedData_wo_Dates_naratives.groupby([cat_target,key_dimn]).size().reset_index(name="counts")    
            cat_summed_counts_df=cat_grouped_conn_df.groupby([cat_target]).sum().reset_index()  #total value counts in each class    for _,cvalues1 in values_df.iterrows():
            for _,cvalues1 in cat_summed_counts_df.iterrows():
                #print(cvalues1)         
                tempdf= cat_grouped_conn_df.loc[cat_grouped_conn_df[cat_target].isin([cvalues1[cat_target]])]
                tempdf['percentage']=(tempdf['counts']/cvalues1['counts'])*100
                q75, q25 = np.percentile(tempdf['percentage'], [75,25])
                
                df_ltq25=tempdf.loc[tempdf['percentage']<q25]
                df_gtq75=tempdf.loc[tempdf['percentage']>=q75]
                            
                key_dim_q25_q75_values=set(list(df_ltq25[key_dimn].unique())+list(df_gtq75[key_dimn].unique()))
                level2df=selectedData_wo_Dates_naratives.loc[selectedData_wo_Dates_naratives[key_dimn].isin(list(key_dim_q25_q75_values))]
                cat_grouped_level2df=level2df.groupby([cat_target,key_dimn,column_values['connected_dimn_1']]).size().reset_index(name="counts")
                for dim1 in key_dim_q25_q75_values: 
                    tempdf_level2= cat_grouped_level2df.loc[(cat_grouped_level2df[cat_target].isin([cvalues1[cat_target]]))&(cat_grouped_level2df[key_dimn].isin([dim1]))]
                    tempdf_level2['percentage']=(tempdf_level2['counts']/cvalues1['counts'])*100
                    level2q75, level2q25 = np.percentile(tempdf_level2['percentage'], [75,25])
                    level2df_ltq25=tempdf_level2.loc[tempdf_level2['percentage']<level2q25]
                    level2df_gtq75=tempdf_level2.loc[tempdf_level2['percentage']>=level2q75]
                    if len(level2df_ltq25)>0:
                        #print('level2df_ltq25')
                        level2narrativedflt25q=pd.DataFrame()
                        for index,rows in level2df_ltq25.iterrows():
                          level2narrativedflt25q=level2narrativedflt25q.append({'metric':rows.index[0],'metric_value':rows[0],'dim1':rows.index[1],'dim1_value':rows[1],'dim2':rows.index[2],'dim2_value':rows[2],'counts':int(rows[3]),'total counts':int(cvalues1['counts']),'percentage':rows[4]  },ignore_index=True)  
                        level2narrativedflt25q['narratives']= level2narrativedflt25q.apply(lambda row: Create_Cat_MetricNarrativeLT25_level2(row,entity), axis=1)
                        level2final_narratives=level2final_narratives.append(level2narrativedflt25q)
                    
                    if len(level2df_gtq75)>0:
                        #print('level2df_gtq75')
                        level2narrativedfgt75q=pd.DataFrame()
                        for index,rows in level2df_gtq75.iterrows():
                          level2narrativedfgt75q=level2narrativedfgt75q.append({'metric':rows.index[0],'metric_value':rows[0],'dim1':rows.index[1],'dim1_value':rows[1],'dim2':rows.index[2],'dim2_value':rows[2],'counts':int(rows[3]),'total counts':int(cvalues1['counts']),'percentage':rows[4]  },ignore_index=True)  
                        level2narrativedfgt75q['narratives']= level2narrativedfgt75q.apply(lambda row: Create_Cat_MetricNarrativeGT75_level2(row,entity), axis=1)
                        level2final_narratives=level2final_narratives.append(level2narrativedfgt75q)
                    #print('key_dimn',key_dimn,'conn1',column_values['connected_dimn_1'])
                    level2df_gtq75["combinedcol"]=level2df_gtq75[key_dimn].astype(str)+"--"+level2df_gtq75[column_values['connected_dimn_1']].astype(str)
                    level2df_ltq25["combinedcol"]=level2df_ltq25[key_dimn].astype(str)+"--"+level2df_ltq25[column_values['connected_dimn_1']].astype(str)
                    
                    level2key_dim_q25_q75_values=set(list(level2df_ltq25['combinedcol'].unique())+list(level2df_gtq75['combinedcol'].unique()))
                    
                    df_temp=selectedData_wo_Dates_naratives.copy()
                    df_temp["combinedcol"]=df_temp[key_dimn].astype(str)+"--"+df_temp[column_values['connected_dimn_1']].astype(str)
                    
                    level3df=df_temp.loc[df_temp['combinedcol'].isin(list(level2key_dim_q25_q75_values))]
                    try:#some times connected_dim_2 is  or None
                        cat_grouped_level3df=level3df.groupby([cat_target,key_dimn,column_values['connected_dimn_1'],column_values['connected_dimn_2']]).size().reset_index(name="counts")
                        cat_grouped_level3df["combinedcol"]=cat_grouped_level3df[key_dimn].astype(str)+"--"+cat_grouped_level3df[column_values['connected_dimn_1']].astype(str)
                        
                        for conndim2 in level2key_dim_q25_q75_values:               
                            tempdf_level3= cat_grouped_level3df.loc[(cat_grouped_level3df[cat_target].isin([cvalues1[cat_target]]))&(cat_grouped_level3df['combinedcol'].isin([conndim2]))]
                            tempdf_level3.drop('combinedcol',axis=1,inplace=True)
                            tempdf_level3['percentage']=(tempdf_level3['counts']/cvalues1['counts'])*100
                            level3q75, level3q25 = np.percentile(tempdf_level3['percentage'], [75,25])
                            level3df_ltq25=tempdf_level3.loc[tempdf_level3['percentage']<level3q25]
                            level3df_gtq75=tempdf_level3.loc[tempdf_level3['percentage']>=level3q75]
                            
                            if len(level3df_ltq25)>0:
                                #print('level3df_ltq25')
                                level3narrativedflt25q=pd.DataFrame()
                                for index,rows in level3df_ltq25.iterrows():
                                  level3narrativedflt25q=level3narrativedflt25q.append({'metric':rows.index[0],'metric_value':rows[0],'dim1':rows.index[1],'dim1_value':rows[1],'dim2':rows.index[2],'dim2_value':rows[2],'dim3':rows.index[3],'dim3_value':rows[3],'counts':int(rows[4]),'total counts':int(cvalues1['counts']),'percentage':rows[5]  },ignore_index=True)  
                                level3narrativedflt25q['narratives']= level3narrativedflt25q.apply(lambda row: Create_Cat_MetricNarrativeLT25_level3(row,entity), axis=1)
                                level3final_narratives=level3final_narratives.append(level3narrativedflt25q)
                            
                            if len(level3df_gtq75)>0:
                                #print('level3df_gtq75')
                                level3narrativedfgt75q=pd.DataFrame()
                                for index,rows in level3df_gtq75.iterrows():
                                  level3narrativedfgt75q=level3narrativedfgt75q.append({'metric':rows.index[0],'metric_value':rows[0],'dim1':rows.index[1],'dim1_value':rows[1],'dim2':rows.index[2],'dim2_value':rows[2],'dim3':rows.index[3],'dim3_value':rows[3],'counts':int(rows[4]),'total counts':int(cvalues1['counts']),'percentage':rows[5]  },ignore_index=True)  
                                level3narrativedfgt75q['narratives']= level3narrativedfgt75q.apply(lambda row: Create_Cat_MetricNarrativeGT75_level3(row,entity), axis=1)
                                level3final_narratives=level3final_narratives.append(level3narrativedfgt75q)
                    except TypeError as e:
                        error_type=e.args
        
        except KeyError as e:
            error_key=e.args
    return level2final_narratives,level3final_narratives


            
def Create_Cat_MetricNarrativeGT75_level3(row,entity):
    col0=row['metric']#metric
    col1=row['metric_value']#
    col2=row['dim1']#dim1
    col3=row['dim1_value']#dim1_value
    col4=row['counts']#counts
    col5=row['percentage']#percentage
    col6=row['total counts']
    col7=row['dim2']
    col8=row['dim2_value']
    col9=row['dim3']
    col10=row['dim3_value']
    
    col5=round(col5,2)

    narrative= "'"+str(int(col4)) +"' out of '"+str(int(col6))+"' i.e "+str(col5)+"%' of "+entity+" where '"+str(col0)+"' is '"+str(col1)+"' are when '"+str(col2)+"' is '"+str(col3)+"' , '"+str(col7)+"' is '"+str(col8)+"' and '"+str(col9)+"' is '"+str(col10)+"'"
    return narrative

def Create_Cat_MetricNarrativeLT25_level3(row,entity):
    col0=row['metric']#metric
    col1=row['metric_value']#
    col2=row['dim1']#dim1
    col3=row['dim1_value']#dim1_value
    col4=row['counts']#counts
    col5=row['percentage']#percentage
    col6=row['total counts']
    col7=row['dim2']
    col8=row['dim2_value']
    col9=row['dim3']
    col10=row['dim3_value']
    
    col5=round(col5,2)

    
    narrative= "Just '"+str(int(col4)) +"' out of '"+str(int(col6))+"' i.e '"+str(col5)+"%' of "+entity+" where '"+str(col0)+"' is '"+str(col1)+"' are when '"+str(col2)+"' is '"+str(col3)+"' , '"+str(col7)+"' is '"+str(col8)+"' and '"+str(col9)+"' is '"+str(col10)+"'"

 

    # narrative_dict={'dim1':col1,"dim1_value":row[col1],
    #                 'metric':col0,'metric_value':row[col1],'percentage':row[col3],'narratives':narrative} 
    return narrative
            
        
def Create_Cat_MetricNarrativeGT75_level2(row,entity):
    col0=row['metric']#metric
    col1=row['metric_value']#
    col2=row['dim1']#dim1
    col3=row['dim1_value']#dim1_value
    col4=row['counts']#counts
    col5=row['percentage']#percentage
    col6=row['total counts']
    col7=row['dim2']
    col8=row['dim2_value']
    

    col5=round(col5,2)

    
    narrative= "'"+str(int(col4)) +"' out of '"+str(int(col6))+"' i.e '"+str(col5)+"%' of "+entity+" where '"+str(col0)+"' is '"+str(col1)+"' are when '"+str(col2)+"' is '"+str(col3)+"' and '"+str(col7)+"' is '"+str(col8)+"'"

 

    # narrative_dict={'dim1':col1,"dim1_value":row[col1],
    #                 'metric':col0,'metric_value':row[col1],'percentage':row[col3],'narratives':narrative} 
    return narrative

def Create_Cat_MetricNarrativeLT25_level2(row,entity):
    col0=row['metric']#metric
    col1=row['metric_value']#
    col2=row['dim1']#dim1
    col3=row['dim1_value']#dim1_value
    col4=row['counts']#counts
    col5=row['percentage']#percentage
    col6=row['total counts']
    col7=row['dim2']
    col8=row['dim2_value']
    col5=round(col5,2)

    
    narrative= "Just '"+str(int(col4)) +"' out of '"+str(int(col6))+"' i.e '"+str(col5)+"%' of "+entity+" where '"+str(col0)+"' is '"+str(col1)+"' are when '"+str(col2)+"' is '"+str(col3)+"' and '"+str(col7)+"' is '"+str(col8)+"'"

 

    # narrative_dict={'dim1':col1,"dim1_value":row[col1],
    #                 'metric':col0,'metric_value':row[col1],'percentage':row[col3],'narratives':narrative} 
    return narrative
            


def Create_Cat_MetricNarrativeGT75(row,entity):
    col0=row['metric']#metric
    col1=row['metric_value']#
    col2=row['dim1']#dim1
    col3=row['dim1_value']#dim1_value
    col4=row['counts']#counts
    col5=row['percentage']#percentage
    col6=row['total counts']

    col5=round(col5,2)

    
    narrative= "'"+str(int(col4)) +"' out of '"+str(int(col6))+"' i.e '"+str(col5)+"%' of "+entity+" where '"+str(col0)+"' is '"+str(col1)+"' are when '"+str(col2)+"' is '"+str(col3)+"'"

 

    # narrative_dict={'dim1':col1,"dim1_value":row[col1],
    #                 'metric':col0,'metric_value':row[col1],'percentage':row[col3],'narratives':narrative} 
    return narrative

def Create_Cat_MetricNarrativeLT25(row,entity):
    col0=row['metric']#metric
    col1=row['metric_value']#
    col2=row['dim1']#dim1
    col3=row['dim1_value']#dim1_value
    col4=row['counts']#counts
    col5=row['percentage']#percentage
    col6=row['total counts']
    col5=round(col5,2)

    
    narrative= "Just '"+str(int(col4)) +"' out of '"+str(int(col6))+"' i.e '"+str(col5)+"%' of "+entity+" where '"+str(col0)+"' is '"+str(col1)+"' are when '"+str(col2)+"' is '"+str(col3)+"'"

 

    # narrative_dict={'dim1':col1,"dim1_value":row[col1],
    #                 'metric':col0,'metric_value':row[col1],'percentage':row[col3],'narratives':narrative} 
    return narrative

#################################################hard coded text functions ##################################
def TextForNoQuarterNarratives():
    narrative=    [
				{
					"year1": "N/A",
					"year1_quarter": "N/A",
					"year2": "N/A",
					"year2_quarter": "N/A",
					"percentage": "N/A",
					"type": "N/A",
					"narratives": " Inference is not generated as data is not available for the same quarter across years"
				}]
    return narrative

def TextForNoMonthNarratives():
    narrative=    [
				{
					"year1": "N/A",
					"year1_month": "N/A",
					"year2": "N/A",
					"year2_month": "N/A",
					"percentage": "N/A",
					"type": "N/A",
					"narratives": " Inference is not generated as data is not available for the same month across years"
				}]
    return narrative



def TextForNoDayPredictionNarratives():
    narrative=    [ {
					"day": "N/A",
					"prediction_value": "N/A",
					"type": "N/A",
					"narratives": "Minimum 3 days data is required to generate inferences"
				}]
    return narrative

def TextForNoMonthPredictionNarratives():
    narrative=    [ {
					"year": "N/A",
					"month": "N/A",
					"prediction_value": "N/A",
					"type": "N/A",
					"narratives": "Minimum 3 months data is required to generate inferences"
				}]
    return narrative

def TextForNoQuarterPredictionNarratives():
    narrative=    [ {
					"year": "N/A",
					"quarter": "N/A",
					"prediction_value": "N/A",
					"type": "N/A",
					"narratives": "Minimum 3 quarters data is required to generate inferences"
				}]
    return narrative


def TextForNoWeekPredictionNarratives():
    narrative=    [ {
					"week": "N/A",
					"prediction_value": "N/A",
					"type": "N/A",
					"narratives": "Minimum 3 Weeks data is required to generate inferences"
				}]
    return narrative


def TextForNoLevel1DN():
    narrative=    [ {
					"col_a": "N/A",
					"col_a_value": "N/A",
					"percentage": "N/A",
					"narratives": "No  attributes are available to generate Distributive Analysis inferences"
				}]
    return narrative

def TextForNoLevel2DN():
    narrative=    [ {
					"col_a": "N/A",
					"col_a_value": "N/A",
                    "col_b": "N/A",
					"col_b_value": "N/A",
					"percentage": "N/A",
					"narratives": "No correlated attributes are available to generate Distributive Analysis inferences"
				}]
    return narrative

def TextForNoLevel2MeasureNarratives(targetfornarrative):
    narrative=    [ {
                    "dim1": "N/A",
					"dim1_value": "N/A",
					"dim2": "N/A",
					"dim2_value": "N/A",
					"metric": targetfornarrative,
					"metric_value": "N/A",
					"percentage": 'N/A',
					"X_times": 'N/A',
					"type": "N/A",
					"narratives": "No correlated attributes are available to generate Inferences on measure"}]
    return narrative

def TextForNoLevel3MeasureNarratives(targetfornarrative):
    narrative=    [ {
                    "dim1": "N/A",
					"dim1_value": "N/A",
					"dim2": "N/A",
					"dim2_value": "N/A",
                    "dim3": "N/A",
					"dim3_value": "N/A",
					"metric": targetfornarrative,
					"metric_value": "N/A",
					"percentage": 'N/A',
					"X_times": 'N/A',
					"type": "N/A",
					"narratives": "No correlated attributes are available to generate Inferences on measure"}]
    return narrative

def TextForNoLevel1ContinousOutliers(targetfornarrative):
    narrative=  [{
                    "metric":targetfornarrative ,
                    "metric_value": "N/A",
                    "type": "N/A",
                    "dim1": "N/A",
                    "dim1_value": "N/A",
                    "narratives": "No outliers detected for the selected measure"
                     }]
    return narrative

def TextForNoLevel2ContinousOutliers(targetfornarrative):
    narrative=  [{
                                    "metric": targetfornarrative,
                                    "metric_value": "N/A",
                                    "type": "N/A",
                                    "dim1": "N/A",
                                    "dim1_value": "N/A",
                                    "dim2": "N/A",
                                    "dim2_value": "N/A",
                                    "narratives": "No outliers detected for the selected measure"
                     }]
    return narrative


def TextForNoLevel3ContinousOutliers(targetfornarrative):
    narrative=  [{
                                    "metric": targetfornarrative,
                                    "metric_value": "N/A",
                                    "type": "N/A",
                                    "dim1": "N/A",
                                    "dim1_value": "N/A",
                                    "dim2": "N/A",
                                    "dim2_value": "N/A",
                                    "dim3": "N/A",
                                    "dim3_value": "N/A",
                                    "narratives": "No outliers detected for the selected measure"
                     }]
    return narrative

list_narr=["When Categorical Variable type is selected as metric outliers will not be provided",
            "For Categorical Variable Type Selection as metric, outlier are not offered",
            "Upon the condition where Categorical Variable type is selected as metric, outlier will not be provided",
            "Subject to metric selected as Categorical type, outlier will not be provided",
            "Outlier are not provided when metric is selected as Categorical variable type",
            "If Metric is selected as Categorical Variable Type,outlier are not provided",
            "Subject to Metric value selection as Categorical Variable Type, outlier will not be provided"]

def TextForNoLevel1CategoryOutliers(targetfornarrative):
    narrative=  [{
                                    "metric":targetfornarrative ,
                                    "metric_value": "N/A",
                                    "type": "N/A",
                                    "dim1": "N/A",
                                    "dim1_value": "N/A",
                                    "narratives": random.choice(list_narr)

                     }]
    return narrative


def TextForNoLevel2CategoryOutliers(targetfornarrative):
    narrative=  [{
                "metric": targetfornarrative,
                                    "metric_value": "N/A",
                                    "type": "N/A",
                                    "dim1": "N/A",
                                    "dim1_value": "N/A",
                                    "dim2": "N/A",
                                    "dim2_value": "N/A",
                                    "narratives": random.choice(list_narr)

                     }]
    return narrative


def TextForNoLevel3CategoryOutliers(targetfornarrative):
    narrative=  [{                   "metric": targetfornarrative,
                                    "metric_value": "N/A",
                                    "type": "N/A",
                                    "dim1": "N/A",
                                    "dim1_value": "N/A",
                                    "dim2": "N/A",
                                    "dim2_value": "N/A",
                                    "dim3": "N/A",
                                    "dim3_value": "N/A",
                                    "narratives": random.choice(list_narr)

                     }]
    return narrative




    