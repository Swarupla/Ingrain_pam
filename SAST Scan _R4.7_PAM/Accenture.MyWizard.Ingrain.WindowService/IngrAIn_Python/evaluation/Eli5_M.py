# -*- coding: utf-8 -*-
"""
Created on Thu Jan 24 16:49:51 2019

@author: sravan.kumar.tallozu
"""
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
import eli5
from collections import OrderedDict
import operator
import lime
import lime.lime_tabular
import functools
def eli5_m(estimator,X_eli5,problemType):
    
    explain_weights = eli5.explain_weights(estimator, feature_names = X_eli5.columns.tolist())
    
    explain_weights_dict=eli5.formatters.as_dict.format_as_dict(explain_weights)
    #print (explain_weights_dict)
    if (explain_weights_dict.get('method')=="linear model"):
        feature_importance_dict = explain_weights_dict.get('targets')[0].get('feature_weights').get('pos')
        feature_name  = [i['feature'] for i in feature_importance_dict if 'feature' in i]
        weights= [i['weight'] for i in feature_importance_dict if 'weight' in i]

        feature_importance_dict = explain_weights_dict.get('targets')[0].get('feature_weights').get('neg')
        feature_name.extend([i['feature'] for i in feature_importance_dict if 'feature' in i])
        weights.extend([i['weight'] for i in feature_importance_dict if 'weight' in i])
        
    #print (feature_name,weights)
    
    elif ('error' in explain_weights_dict.keys()) and (explain_weights_dict['error'] != None):
        truncateSize = 50 if X_eli5.shape[0] > 50 else X_eli5.shape[0]        
        if problemType == "classification" or problemType=="Multi_Class":
            predict_fn = lambda x: estimator.predict_proba(x).astype(float)
        else:
            predict_fn = lambda x: estimator.predict(x).astype(float)
        columnNames = X_eli5.columns.tolist()
        explainer = lime.lime_tabular.LimeTabularExplainer(X_eli5.values,mode = 'classification' if problemType=='Multi_Class' else problemType,feature_names = columnNames)   
        importances={k:0.0 for k in columnNames}
        for indx in range(0,X_eli5[0:truncateSize].shape[0]):
            exp = explainer.explain_instance(X_eli5.values[indx],predict_fn, num_features=X_eli5.shape[1])
            exp_map = exp.as_map()
            feat = [exp_map[1][m][0] for m in range(len(exp_map[1]))]
            weight = [exp_map[1][m][1] for m in range(len(exp_map[1]))]
            for m in range(len(feat)):
                    importances[columnNames[feat[m]]] = importances[columnNames[feat[m]]] + weight[m] 
        for i in columnNames:
            importances[i] = importances[i] / (X_eli5[0:truncateSize].shape[0]*1.0)
                                               
        importances={k:importances[k]+(1-min(importances.values())) for k in importances}                                             
        #print (importances)
        return importances
        
##        print ("Started")
##        print  (X_eli5.shape)
##        truncateSize = 50 if X_eli5.shape[0] > 50 else X_eli5.shape
##        kernel_explainer = shap.KernelExplainer(estimator.predict,X_eli5[0:truncateSize])
##        truncateSize1 = 25 if test.shape[0] > 25 else test.shape
##        shap_values = kernel_explainer.shap_values(test[0:truncateSize1])
##        weights = [each/shap_values.shape[0] for each in functools.reduce(lambda x,y:x+y,shap_values)]
##        feature_name = X_eli5.column
##        print (weights,feature_name)
##        print ("Ended")
        
    else:    
        feature_importance_dict=explain_weights_dict.get('feature_importances').get('importances')
        feature_name = [i['feature'] for i in feature_importance_dict if 'feature' in i]
        weights= [i['weight'] for i in feature_importance_dict if 'weight' in i]


    dictionary=dict(zip(feature_name,weights))
    dictionary={k:round(v,2) for k,v in dictionary.items()}
 
    
    
    return dictionary



