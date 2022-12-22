# -*- coding: utf-8 -*-
"""
Created on Thu Mar 21 18:14:25 2019

@author: sravan.kumar.tallozu
"""

''' 
Missing Values Imputer, used for both numeric and categorical data
for Numeric data use either mean, median, most_frequent, constant
for Categorical data use most_frequent or constant
for Numerical data
    method = median
    data_imputer(data,method)
for Categorical data
    method = most_frequent
    data_imputer(data,method)


data = pd.DataFrame([
       ['Successful', 'a1', 1],
       ['b',          'b1', 2], 
       ['c',          'c1', 3],
       [np.nan,        np.nan, np.nan]])
data.columns = ['ChangeOutcome', 'Category', 'EffortsInMinutes']
'''

import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
from sklearn.impute import SimpleImputer
import pandas as pd
import numpy as np

def data_imputer(data,method,cols,constant=None):       
    #for Missing Values having having nan
    data_t = data[[cols]]       
    imputer = SimpleImputer(missing_values=np.nan, strategy=method, fill_value=constant)
    imputer = imputer.fit(data_t)
    imputed_df = imputer.transform(data_t)
    data[cols] = pd.DataFrame(data=imputed_df[:,:],   
                              index=data_t.index,    
                              columns=data_t.columns)
    #for Missing Values having having None
    if data[cols].isnull().any():
        data_t = data[[cols]]       
        imputer = SimpleImputer(missing_values=None, strategy=method, fill_value=constant)
        imputer = imputer.fit(data_t)
        imputed_df = imputer.transform(data_t)
        data[cols] = pd.DataFrame(data=imputed_df[:,:],   
                                  index=data_t.index,    
                                  columns=data_t.columns)
    
    return data