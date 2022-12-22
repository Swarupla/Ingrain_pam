# -*- coding: utf-8 -*-
"""
Created on Fri Jul 10 11:35:03 2020

@author: shrayani.mondal
"""
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
#Load Required Packages
import pandas as pd
import numpy as np
from sklearn.preprocessing import LabelEncoder

le = LabelEncoder()

def woe(data, target, bins=10, cat_cols=[], numerical_cols=[]):
    
    #make sure that the data has no NaN value
    data.dropna(inplace=True)
    data.reset_index(drop=True, inplace=True)
    
    #dictionary to keep track of variable bins
    bins_dict = {}
    
    #Define num and Categorical Column Names
    cols = cat_cols + numerical_cols
    
    #remove the target from cols if present
    if target in cols: cols.remove(target)
    
    #convert the target column into label encoded column
    data[target + "_encoded"] = le.fit_transform(data[target].astype(str))
    
    #Run WOE on all the independent variables
    for ivars in cols:
        #print(ivars)
        #if (data[ivars].dtype.kind in 'bifc') and (len(np.unique(data[ivars]))>10):
        if (ivars in numerical_cols): #and (len(np.unique(data[ivars]))>10):
            binned_x = pd.qcut(data[ivars], bins,  duplicates='drop')
            d0 = pd.DataFrame({'x': binned_x, 'y': data[target + "_encoded"]})
        elif (ivars in cat_cols):
            d0 = pd.DataFrame({'x': data[ivars], 'y': data[target + "_encoded"]})
            
        d0['x_encoded'] = le.fit_transform(d0['x'].astype(str))
        d = d0.groupby("x", as_index=False).agg({"y": ["count", "sum"]})
        d.columns = ['Cutoff', 'N', 'Events']
        d['% of Events'] = np.maximum(d['Events'], 0.5) / d['Events'].sum()
        d['Non-Events'] = d['N'] - d['Events']
        d['% of Non-Events'] = np.maximum(d['Non-Events'], 0.5) / d['Non-Events'].sum()
        d['WoE'] = np.log(d['% of Events']/d['% of Non-Events'])
        d.insert(loc=0, column='Variable', value=ivars)
                    
        #dictionary creation to keep track of bins
        #16.7 in woeDF['Cutoff'][0] ---> checks if a value is in an interval or not
        
        temp_dict = {}
        for i in range(0,d.shape[0]):
            #print(i)
            temp_dict[d['Cutoff'][i]] = d0[d0["x"]==d['Cutoff'][i]]["x_encoded"].iloc[0]
        
        #merge the bins with similar woe           
        temp_df = d[["Cutoff","WoE","N"]]
        temp_df["N_dist"] = temp_df["N"]/temp_df["N"].sum()
        
        #temp_df = temp_df[temp_df["Variable"]== ivars]
        sorted_temp_df = temp_df.sort_values(by=['WoE'])
        sorted_temp_df.reset_index(drop=True, inplace=True)
    
        # iterate through each row and select  
        # map the intervals with close WoE(<=0.05) to same labels in bins_dict
        #merge bins only if the number of bins formed is greater than 5
        #and the range of values of WoE is atleast 0.05. Or else you will end up 
        #with one single bin
        range_woe = abs(sorted_temp_df['WoE'].max() - sorted_temp_df['WoE'].min())
        if (ivars in numerical_cols):
            if(len(sorted_temp_df) > 5 and range_woe >= 0.05):
                i = 0
                while(i < len(sorted_temp_df)-1) : 
                    j = i+1
                    while(j < len(sorted_temp_df)) :
                        if(abs(sorted_temp_df.loc[i,"WoE"] - sorted_temp_df.loc[j,"WoE"]) < 0.05):
                            temp_dict[sorted_temp_df.loc[j,"Cutoff"]] = temp_dict[sorted_temp_df.loc[i,"Cutoff"]]
                            j = j+1
                        else:
                            break
                    i = j
        
        elif (ivars in cat_cols):
            if(len(sorted_temp_df) > 5 and range_woe >= 0.1):
                i = 0
                while(i < len(sorted_temp_df)-1) : 
                    j = i+1
                    while(j < len(sorted_temp_df)) :
                        if((abs(sorted_temp_df.loc[i,"WoE"] - sorted_temp_df.loc[j,"WoE"]) < 0.1) and (sorted_temp_df.loc[j,"N_dist"]<0.05)):
                            temp_dict[sorted_temp_df.loc[j,"Cutoff"]] = temp_dict[sorted_temp_df.loc[i,"Cutoff"]]
                            j = j+1
                        else:
                            break
                    i = j
        
        if (ivars in numerical_cols):
            temp_list = [key for key in temp_dict.keys()]
            interval_min = min(temp_list)
            interval_max = max(temp_list)
            
            #setting the lower bound of as -infinity
            temp_dict[pd.Interval(left=float('-inf'), right=interval_min.right)] = temp_dict.pop(interval_min)
            
            #setting the upper bound of as +infinity
            temp_dict[pd.Interval(left=interval_max.left, right=float('inf'))] = temp_dict.pop(interval_max)
            
        if(temp_dict !={}):
            if(ivars in numerical_cols):
                bins_dict[ivars] = ['numerical', temp_dict]
            elif(ivars in cat_cols):
                bins_dict[ivars] = ['categorical', temp_dict]
                
    #drop the label encoded target column
    data.drop([target + "_encoded"], axis=1, inplace = True)
    #data.rename(columns={target + "_encoded": target}, inplace = True)
    
    data = bin_data(data, bins_dict, encoded = False)
       
    return bins_dict, data


def bin_data(data, bins_dict, encoded = True):
    data_wf = data.copy()
    for key,value in bins_dict.items():
        dtype = value[0]
        binned_info = value[1].copy()
        if(encoded and dtype == 'numerical'):
            binned_info = encoded_str_to_intervals(binned_info)   
        for key1, value1 in binned_info.items():
            if(dtype == 'numerical'):
                #df['c1'].loc[df['c1'] == 'Value'] = 10 --> replaces in the existing column
                #df.loc[df['c1'] == 'Value', 'c2'] = 10 -->creates a new column c2
                #data.loc[(data.A>=interval.left) & (data.A<interval.right)]
                data_wf.loc[(data_wf[key]>key1.left) & (data_wf[key]<=key1.right), key+"_binned"] = value1
                #data_wf[key+_"binned"] = data_wf[key].assign()
            elif(dtype == 'categorical'):
                try:
                    data_wf.loc[(data_wf[key]==float(key1)), key+"_binned"] = value1
                except:
                    data_wf.loc[(data_wf[key]==key1), key+"_binned"] = value1
        
        #drop this key, rename key_binned as key
        data_wf.drop([key], axis=1, inplace = True)
        data_wf.rename(columns={key+"_binned": key}, inplace = True)
    return data_wf

'''
def encoded_str_to_intervals(encoded_dict):
    result = {}
    for key,value in encoded_dict.items():
        str1 = key
        l,r = str1.split(", ")[0], str1.split(", ")[1]
        if  l.endswith('-inf'):
            result[pd.Interval(left=float('-inf'), right=float(r))] = value
        elif r.endswith('inf'):
            result[pd.Interval(left=float(l.split("(")[1]), right=float('inf'))] = value
        else:
            result[pd.Interval(left=float(l.split("(")[1]), right=float(r))] = value
            
    return result
'''
def encoded_str_to_intervals(encoded_dict):
    result = {}
    for key,value in encoded_dict.items():
        result[key]=value
    return result