# -*- coding: utf-8 -*-
"""
Created on Fri Jan 25 13:16:55 2019

@author: neha.afreen
"""
import pandas as pd
from sklearn.cross_validation import train_test_split
from sklearn.cluster import KMeans
import pickle
import json

from sklearn.model_selection import RandomizedSearchCV

def evaluate_KMeans(Data,predict_column,myhyperParamsKMeans,hyperFlag):
    
    model = KMeans()
    try:
       df = pd.DataFrame(Data)
       x = df.drop(columns=['predict_column'])
       y = df('predict_column')
       
       X_train, X_test, y_train, y_test = train_test_split(x, y, test_size=0.2, random_state=3)
       
       if hyperFlag == True:  
            KMeans = KMeans(n_clusters=myhyperParamsKMeans['n_clusters'],init=myhyperParamsKMeans['init'],n_init=myhyperParamsKMeans['n_init'], max_iter=myhyperParamsKMeans['max_iter'], tol=myhyperParamsKMeans['tol'],precompute_distances=myhyperParamsKMeans['precompute_distances'], 
                           verbose=myhyperParamsKMeans['verbose'], random_state=myhyperParamsKMeans['random_state'],copy_x=myhyperParamsKMeans['copy_x'], n_jobs=myhyperParamsKMeans['n_jobs'], algorithm=myhyperParamsKMeans['algorithm'])
            KMeans.fit(X_train, y_train)
            KMeans_train_score =KMeans.score(X_train,y_train)
            KMeans_test_score = KMeans.score(X_test, y_test)
            outputScore = {"hyperParams": myhyperParamsKMeans,"KMeans_train_score":KMeans_train_score,"KMeans_test_score":KMeans_test_score} 
			pickle.dump(KMeans, open("KMeans.pickel", "wb"))
       else:
            n_clusters=[8,10,12]
            max_iter = ["none",300]
            tol = [0.01, 0.02, 0.05]
            algorithm = ["auto","full","elkan"]
            
            rf_random = RandomizedSearchCV(estimator = model, param_distributions = dict(n_cluster=n_cluster,max_iter=max_iter, tol = tol,algorithm=algorithm),n_iter = 100, cv = 3, verbose=2, random_state=42, n_jobs = -1)
            rf_random.fit(X_train,y_train)
            outputScore = {"hyperParams": rf_random.best_params_,"KMeans_best_score":rf_random.best_score_,"KMeans_error_score":rf_random.error_score}    
			pickle.dump(rf_random, open("rf_random.pickel", "wb"))
			
		OutputScoreJson = json.dumps(outputScore)
		return OutputScoreJson
        
    except Exception as e:
        error = {"error": e}
        errorJason = json.dumps(error)
        return errorJason
        