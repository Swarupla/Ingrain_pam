# -*- coding: utf-8 -*-
"""
Created on Fri Mar 22 15:19:51 2019

@author: sravan.kumar.tallozu
"""
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning)  
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
import pandas as pd
from sklearn import preprocessing
import numpy as np
import re
'''
Label_Encode/Label_Encode_Dec
Arguments : 1 -> DataFrame
            2 -> Features/Columns on which OHE to be performed

Label_Encode
returns   : 1 -> Encoded data concatenated with main DataFrame
            2 -> Encoded data
            3 -> OHE Encoder
Label_Encode_Dec
returns   : 1 -> Encoded data concatenated with main DataFrame
            2 -> Encoded data

df = pd.DataFrame([
       ['a', 'c1', 1],
       ['c', 'b1', 2], 
       ['b', 'a1', 3]])
df.columns = ['alpha', 'alpha_num', 'num']
le_cols =  ['alpha', 'alpha_num']

# Label Encoder
a,b,c = Label_Encode(df,le_cols)
# Lable Encoder Decoder
a1,b1 =  Label_Encode_Dec(c,df,le_cols)
'''
def Label_Encode(data, le_cols):    
    lenc = preprocessing.LabelEncoder() 
    for col in le_cols:
        data[col] = data[col].astype(str)
    lencm = lenc.fit(data[le_cols].values.flatten())
    X = (lencm.transform(data[le_cols].values.flatten())).reshape(data[le_cols].shape)
    enc_cols =  [x +'_' + 'L' for x in le_cols]
    enc_df = pd.DataFrame(X, columns=enc_cols, index=data.index)
    data = pd.concat([data, enc_df], axis=1)
    return data,enc_df,lencm,enc_cols


def Label_Encode_Dec(lencm,data,le_cols):
    try:
        for col in le_cols:
            data[col] = data[col].astype(str)
        X = (lencm.transform(data[le_cols].values.flatten())).reshape(data[le_cols].shape)
        classMapping = dict(zip(lencm.classes_, lencm.transform(lencm.classes_).tolist()))
    except ValueError:
        X = []
        for col in le_cols:
            data[col] = data[col].astype(str)
        classMapping = dict(zip(lencm.classes_, lencm.transform(lencm.classes_).tolist()))
        for each in data[le_cols].values.flatten():
            try:
                X.append(lencm.transform([each]))
            except ValueError as e:
                if re.search(r"\[\'(.*)\'\]", str(e)):                    
                    m = re.search(r"\[\'(.*)\'\]", str(e)).group(1)
                    if m in classMapping:
                        X.append(classMapping[m])
                    else:
                        classMapping[m] = np.array([len(classMapping)],dtype=np.int64)
                        X.append(classMapping[m])
        
        X = np.array(X).reshape(data[le_cols].shape)        
    enc_cols =  [x +'_' + 'L' for x in le_cols]
    enc_df = pd.DataFrame(X, columns=enc_cols, index=data.index)
    data = pd.concat([data, enc_df], axis=1)
    return data,enc_df

def Label_Encode_Dec_modified(lencm,data,le_cols):
    try:
        for col in le_cols:
            data[col] = data[col].astype(str)
        X = (lencm.transform(data[le_cols].values.flatten())).reshape(data[le_cols].shape)
        classMapping = dict(zip(lencm.classes_, lencm.transform(lencm.classes_).tolist()))
    except ValueError:
        X = []
        for col in le_cols:
            data[col] = data[col].astype(str)
        classMapping = dict(zip(lencm.classes_, lencm.transform(lencm.classes_).tolist()))
        for each in data[le_cols].values.flatten():
            try:
                X.append(lencm.transform([each]))
            except ValueError as e:
                if re.search(r"\[\'(.*)\'\]", str(e)):                    
                    m = re.search(r"\[\'(.*)\'\]", str(e)).group(1)
                    if m in classMapping:
                        X.append(np.array([classMapping[m]],dtype=np.int64))
                    else:
                        classMapping[m] = len(classMapping)
                        X.append(np.array([classMapping[m]],dtype=np.int64))
        
        X = np.array(X).reshape(data[le_cols].shape)        
    enc_cols =  [x +'_' + 'L' for x in le_cols]
    enc_df = pd.DataFrame(X, columns=enc_cols, index=data.index)
    data = pd.concat([data, enc_df], axis=1)
    return data,enc_df,classMapping


'''
one_hot/one_hot_dec
Arguments : 1 -> DataFrame
            2 -> Features/Columns on which OHE to be performed

one_hot
returns   : 1 -> Encoded data concatenated with main DataFrame
            2 -> Encoded data
            3 -> OHE Encoder
one_hot_dec
returns   : 1 -> Encoded data concatenated with main DataFrame
            2 -> Encoded data
            
            
df = pd.DataFrame([
       ['a', 'a1', 1],
       ['b', 'b1', 2], 
       ['c', 'c1', 3]])
df.columns = ['alpha', 'alpha_num', 'num']

ohe_cols =  ['alpha', 'alpha_num']

# data one hot encoding
data_enc,enc_data,enc_model,uniq_vals = one_hot(df,ohe_cols)    

df1 = pd.DataFrame([['a', 'a1', 1]])
df1.columns = ['alpha', 'alpha_num', 'num']

# data one hot encoding from model
data_encD,enc_dfD = one_hot_dec(enc_model,df1,ohe_cols)


'''
#def one_hot(data, ohe_cols):         
#    ohenc = preprocessing.OneHotEncoder(handle_unknown='ignore',sparse=False,categorical_features=None, categories=None,n_values=None)
#    ohem = ohenc.fit(data[ohe_cols].astype(str))
#    X = ohem.transform(data[ohe_cols].astype(str))
#    uniq_vals = data[ohe_cols].apply(lambda x: x.value_counts()).unstack()
#    uniq_vals1 = uniq_vals[~uniq_vals.isnull()]
#    enc_cols = list(uniq_vals1.index.map('{0[0]}_{0[1]}'.format)) 
#    enc_df = pd.DataFrame(X, columns=enc_cols, index=data.index)
#    data = pd.concat([data, enc_df], axis=1)
#    return data,enc_df,ohem,

def getColumnsNames(data,ohem):
    cols= []
    for col,catArray in zip(data.columns,ohem.categories_):
        for cat in catArray:
            cols.append(col+"_"+cat)
    return cols

def one_hot(data, ohe_cols):    
    ohenc = preprocessing.OneHotEncoder(handle_unknown='ignore',sparse=False)
    ohem = ohenc.fit(data[ohe_cols].astype(str))
    X = ohem.transform(data[ohe_cols].astype(str))
    enc_df = pd.DataFrame(X,columns = list(ohem.get_feature_names()))
    enc_cols=getColumnsNames(data[ohe_cols],ohem)
    enc_df.columns = enc_cols
    data = pd.concat([data.reset_index(drop=True), enc_df.reset_index(drop=True)],axis=1)  
    return data,enc_df,ohem,enc_cols

def one_hot_dec(ohem,data,ohe_cols,enc_cols):     
    X = ohem.transform(data[ohe_cols])    
    enc_df = pd.DataFrame(X, columns=enc_cols, index=data.index)
    data = pd.concat([data, enc_df], axis=1)
    return data,enc_df            
   