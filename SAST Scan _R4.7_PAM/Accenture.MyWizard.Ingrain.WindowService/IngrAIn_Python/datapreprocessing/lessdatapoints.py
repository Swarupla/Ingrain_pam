# -*- coding: utf-8 -*-
"""
Created on Thu Jul 22 12:34:03 2021

@author: k.sudhakara.reddy
"""
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
import pandas as pd
import statistics as st
import numpy as np
global num_type,float_type

num_type = [ 'int64', 'int', 'int32']
num_float_type=['float64', 'int64', 'int', 'float', 'float32', 'int32']
float_type=['float64','float', 'float32']
ob_type = ['object']

def minimaldatapoints(data,uniqueIdentifier):
    pd_data=data.copy()
    pd_data = data.append([data]*int(20/len(data.index)),ignore_index=True)
    pd_data = pd_data[pd_data.index<=19]
    for y in data.columns:
        if ((data[y].dtype in num_type) and  (y != uniqueIdentifier)):
            data[y].fillna(0, inplace=True)
            col_arr = data[y]
            col_mean = st.mean(col_arr)
            col_sd   = st.stdev(col_arr)
            random_value = list(np.random.normal(col_mean,col_sd,int(20-len(data))))
            random_to_whole = [abs(round(x)) for x in random_value]
            org_value=list(data[y])
            l3=pd.DataFrame(org_value+random_to_whole,columns=[y])
            pd_data[y] = l3[y].values
        elif ((data[y].dtype in float_type) and  (y != uniqueIdentifier)):
            data[y].fillna(0, inplace=True)
            col_arr = data[y]
            col_mean = st.mean(col_arr)
            col_sd   = st.stdev(col_arr)
            random_value = list(np.random.normal(col_mean,col_sd,int(20-len(data))))
            list_decimal = list(abs(np.around(np.array(random_value),2)))
            org_value=list(data[y])
            l3=pd.DataFrame(org_value+list_decimal,columns=[y])
            pd_data[y] = l3[y].values
            #pd_data[y]= pd_data[y].round(0).astype(int).abs()
    
    for x in data.columns:
        if ((x == uniqueIdentifier) and (data[uniqueIdentifier].dtype not in num_float_type)):
            lastrow=data[x].iloc[-1]
            rp_row = [lastrow+str(i) for i in range(20-len(data))]
            org_row=list(data[x])
            df_data=pd.DataFrame(org_row+rp_row,columns=[x])
            pd_data[x] = df_data[x].values
        elif ((x == uniqueIdentifier) and (data[uniqueIdentifier].dtype in num_float_type)):
            int_last = data[x].iloc[-1]
            rp_row1 = [int_last+i for i in range(1,21-len(data))]
            org_row1=list(data[x])
            df_data1=pd.DataFrame(org_row1+rp_row1,columns=[x])
            pd_data[x] = df_data1[x].values      
            
    return pd_data