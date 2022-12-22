# -*- coding: utf-8 -*-
"""
Created on Fri Mar 22 15:19:51 2019

@author: sravan.kumar.tallozu
"""
     
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


