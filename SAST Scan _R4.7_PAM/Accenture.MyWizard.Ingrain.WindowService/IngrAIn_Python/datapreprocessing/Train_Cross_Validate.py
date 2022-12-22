# -*- coding: utf-8 -*-
"""
Created on Mon Feb 18 16:40:17 2019

@author: sravan.kumar.tallozu
"""
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
import numpy as np
import pandas as pd
from datetime import datetime
from sklearn.model_selection import RandomizedSearchCV
from sklearn.metrics import roc_auc_score
from sklearn.model_selection import StratifiedKFold
from sklearn.metrics import make_scorer
from sklearn.metrics import r2_score,mean_squared_error
    

import statsmodels.api as sm

def random_search_CV(model,x_data,y_data,params,folds,n_iter,scoring,n_jobs,random_state=None, shuffle=None,counter=3):  
    
    try:
    #skf = StratifiedKFold(n_splits=folds, shuffle = True, random_state = random_state)
        if folds==1:
            folds=2
        if scoring:
            rs_model = RandomizedSearchCV(model, param_distributions=params, n_iter=n_iter, 
                            scoring=scoring, n_jobs=n_jobs, cv=folds, verbose=0, random_state=random_state)   
        else:
            rs_model = RandomizedSearchCV(model, param_distributions=params, n_iter=n_iter, 
                            scoring=make_scorer(mean_squared_error),
                            n_jobs=n_jobs, cv=folds, verbose=0, random_state=random_state)   
        if model == "LogisticRegression()":
            sm.MNLogit(y_data,x_data).fit(method='bfgs')
            return sm.MNLogit
        else:
            rs_model.fit(x_data,y_data)
            return rs_model
    except ValueError:
         if counter>0:
             counter=counter-1
             random_search_CV(model,x_data,y_data,params,folds,n_iter,scoring,n_jobs,None, None,counter)
             


#folds = 4
#n_iter = 10
#scoring= 'accuracy'
#n_jobs= -1
    


#def cross_validate(model,data,folds,random_state=None, shuffle=None):        
#
##Plot the graph for every loop    
#def train_cross_validate(model,data,folds,random_state=None, shuffle=None):        
#
#def grid_search_CV(model,data,folds,random_state=None, shuffle=None):
    
