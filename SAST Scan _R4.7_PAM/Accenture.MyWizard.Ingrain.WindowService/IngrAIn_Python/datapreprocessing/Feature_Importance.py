# -*- coding: utf-8 -*-
"""
Created on Wed Apr  3 23:09:25 2019

@author: sravan.kumar.tallozu
"""
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
import pandas as pd
import regex as re
import numpy as np
import math
from sklearn.ensemble import RandomForestClassifier
from sklearn.ensemble import RandomForestRegressor

def feature_importance(problemtype,x_data,y_data,data_cols):  
    #print(problemtype+'***')
    if problemtype=='Regression':
        model=RandomForestRegressor()
    elif problemtype=='Classification' or problemtype=='Multi_Class' or problemtype == 'Text_Classification':
        model=RandomForestClassifier()
    elif problemtype=="TimeSeries":
        return pd.DataFrame({'Features': data_cols, 'Importance':[1]*len(data_cols)})
    
    model.fit(x_data,y_data)    
    feature_importances = pd.DataFrame(model.feature_importances_,
                                   index = x_data.columns,
                                    columns=['importance']).sort_index()
                                                   
    data_cols = data_cols.sort_values()
#    temp={}  
    FI_Cols = []
    FImp=[]
#    data_FI=pd.DataFrame({'Features': [], 'Importance': []})
    for i in data_cols:      
       i_t = str(i) + '_'       
       col_fimp = []   
       for j,k in feature_importances.iterrows():             
           if re.search(i_t,j):                                 
               col_fimp.append(k[0])
           elif i == j:
               col_fimp.append(k[0])       
       FI = np.median(col_fimp) if not math.isnan(np.median(col_fimp)) else 0
       FI_Cols.append(i)
       FImp.append(FI)       
#       temp.update({i:FI})
    Feat_Imp = pd.DataFrame({'Features': FI_Cols, 'Importance':FImp })
    Feat_Imp = Feat_Imp.sort_values('Importance',ascending=False)
    return Feat_Imp