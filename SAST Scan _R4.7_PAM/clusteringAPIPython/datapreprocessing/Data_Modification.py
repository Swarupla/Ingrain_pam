# -*- coding: utf-8 -*-
"""
Created on Tue Mar 12 18:45:24 2019

@author: sravan.kumar.tallozu
"""
'''
Missing Value
Drop rows
Drop columns
Impute with mean, mediean, mode, constant

Data Encoding
One Hot
Lable
Dummy

Data Modification
Binning
Normalization - max, l1, l2 
feature scaling - MinMax Scaling, Standardization, robust scaler
Log transformation
Outlier Handling IQR,DBSCAN,PYOD

Prescriptions
SMOTE
'''
from sklearn import preprocessing
import numpy as np
import pandas as pd

def StandardScaler(data):   
    scaler = preprocessing.StandardScaler().fit(data)
    data = scaler.transform(data)    #.reshape(1, -1)
    return data,scaler


def MinMaxScaler(data):
    scaler = preprocessing.MinMaxScaler().fit(data)    
    data = scaler.transform(data)
    return data,scaler
 
def RobustScaler(data):
    scaler = preprocessing.RobustScaler().fit(data)
    data = scaler.transform(data.values.reshape(-1, 1))
    return data,scaler

def Normalizer(data):
    scaler = preprocessing.Normalizer().fit(data)
    data = scaler.transform(data)
    return data,scaler


def scaler(data,column,scaler):        
    if scaler is None: scaler = 'MinMaxScaler'
    if scaler == 'StandardScaler':
        data_t,scaler = StandardScaler(data[column].values.reshape(-1,1))
    elif scaler == 'MinMaxScaler':
        data_t,scaler = MinMaxScaler(data[column])
    elif scaler == 'RobustScaler':
        data_t,scaler = RobustScaler(data[column])
    elif scaler == 'Normalizer':
        data_t,scaler = Normalizer(data[column].values.reshape(1,-1))
        data_t=data_t.reshape(-1,1)
    data_t = pd.DataFrame(data_t.tolist(), columns=[column])
    data = data.drop(column,axis =1)
#    data = pd.concat([data,data_t],axis=1,ignore_index=True)  
    data = pd.concat([data.reset_index(drop=True), data_t.reset_index(drop=True)],axis=1)
    return data,scaler

def scaler_dec(data,column,scaler,transm):          
    if scaler is None: scaler = 'MinMaxScaler'
    if scaler == 'StandardScaler':
        data_t = transm.transform(data[column].values.reshape(-1,1))
    elif scaler == 'MinMaxScaler':
        data_t = transm.transform(data[column])
    elif scaler == 'RobstScaler':
        data_t = transm.transform(data[column])
    elif scaler == 'Normalizer':
        data_t = transm.transform(data[column].values.reshape(-1,1))       
    data_t = pd.DataFrame(data_t.tolist(), columns=[column])
    data = data.drop(column,axis =1)
    data = pd.concat([data,data_t],axis=1)              
    return data

'''
data = pd.DataFrame({'a': [0, 1, 2.3, 3.8], 
                   'b': [4, 5, np.nan, 7.9], 
                   'c': [8, 9, 10, -11]})
cols = ['a','b']
data = Log_Trans(data,cols)
'''
def Log_Trans(data,columns):
    #when passed multiple columns
#    data_log = data[columns].applymap(lambda x: np.log(x+1)    
#    data_log.columns = 'log_' + data_log.columns
    #when passed only a single column
    data_log = data[columns].apply(lambda x: np.log(x+1)) 
    data_log = data_log.to_frame()
#    data_log.columns = ['log_' + str(columns)]
    data = data.drop(columns,axis=1)
    data = pd.concat([data,data_log],axis=1)
#    df_log.index = df_log.index + 1
    return data

'''
data = pd.DataFrame({'a': [0, 1, 2.3, 3.8], 
                   'b': [4, 5, np.nan, 7.9], 
                   'c': [8, 9, 10, -11]})
cols = ['a','b']
data = Log_Trans(data,cols)
cols = ['log_a','log_b']
data = Anti_LogTrans(data,cols)
'''
def Anti_LogTrans(data,columns):
    #data_AL = data[columns].applymap(lambda x: np.exp(x)-1) <--- sravn's code
    data_AL = data[columns].apply(lambda x: np.exp(x)-1) #<---changes made by shrayani
    data_AL.columns = list(map(lambda x : x.split('_')[1],list(data_AL.columns)))
    data = data.drop(columns,axis=1)
    data = pd.concat([data,data_AL],axis=1)
    
    
def remove_outlier(data, columns,wtd):    
    data_t = data
    if wtd == 'Remove':
        q1 = data_t[columns].quantile(0.25)
        q3 = data_t[columns].quantile(0.75)
        iqr = q3-q1     
        data_t = data_t[~((data_t[columns] < (q1 - 1.5 * iqr)) |(data_t[columns] > (q3 + 1.5 * iqr))).any(axis=1)]
    elif wtd in ['Mean','Median','Mode']:            
        q1 = data_t[columns].quantile(0.25)
        q3 = data_t[columns].quantile(0.75)
        iqr = q3-q1 
        if wtd == 'Mean':
            data_t[columns] = np.where(((data_t[columns] < (q1 - 1.5 * iqr)) |(data_t[columns] > (q3 + 1.5 * iqr))),data_t[columns].mean(),data_t[columns])
        elif wtd == 'Median':
            data_t[columns] = np.where(((data_t[columns] < (q1 - 1.5 * iqr)) |(data_t[columns] > (q3 + 1.5 * iqr))),data_t[columns].median(),data_t[columns])
        elif wtd == 'Mode':   
            data_t[columns] = np.where(((data_t[columns] < (q1 - 1.5 * iqr)) |(data_t[columns] > (q3 + 1.5 * iqr))),data_t[columns].mode()[0],data_t[columns])
#        for i in columns:        
#            q1 = data_t[i].quantile(0.25)
#            q3 = data_t[i].quantile(0.75)
#            iqr = q3-q1 
#            if wtd == 'Mean':
#                data_t[i] = np.where(((data_t[i] < (q1 - 1.5 * iqr)) |(data_t[i] > (q3 + 1.5 * iqr))),data_t[i].mean(),data_t[i])
#            elif wtd == 'Median':
#                data_t[i] = np.where(((data_t[i] < (q1 - 1.5 * iqr)) |(data_t[i] > (q3 + 1.5 * iqr))),data_t[i].median(),data_t[i])
    else:
        q1 = data_t[columns].quantile(0.25)
        q3 = data_t[columns].quantile(0.75)
        iqr = q3-q1 
        data_t[columns] = np.where(((data_t[columns] < (q1 - 1.5 * iqr)) |(data_t[columns] > (q3 + 1.5 * iqr))),wtd,data_t[columns])
#        for i in columns:
#            q1 = data_t[i].quantile(0.25)
#            q3 = data_t[i].quantile(0.75)
#            iqr = q3-q1 
#            data_t[i] = np.where(((data_t[i] < (q1 - 1.5 * iqr)) |(data_t[i] > (q3 + 1.5 * iqr))),wtd,data_t[i])
    return data_t
    

'''
column = 'PrimaryImpactedApplication'    
categories = ['bwise','cpds','cvent','alfresco','workfront']  
col=['cvent','alfresco','workfront']
newcatgry = ['Other']
data = pd.read_excel('C:/Users/sravan.kumar.tallozu/Documents/SSAI/Release Impact Analysis/FM_Data_final.xlsx',index_col=0)    
'''    
def colbin(data,column,categeories,newcatgry):
    data.loc[data[column].isin(categeories),column]=newcatgry
    
    
    
def colbin_threshold(data,feature,threshold,newcatgry):
    subcats = data[feature].value_counts() <= threshold
    classes = list(subcats.index[subcats])
#    data.loc[(data[column].isin(classes)),column]='Other ' + str(column)
    return classes
    






    
        