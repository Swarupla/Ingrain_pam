# -*- coding: utf-8 -*-
"""
Created on Wed Jan 23 11:02:42 2019

@author: sravan.kumar.tallozu
"""

import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
import matplotlib.pyplot as plt
from sklearn.metrics import roc_curve, auc
from sklearn.metrics import confusion_matrix
from sklearn import metrics
import tempfile
import base64
import numpy as np
from SSAIutils import utils

#Pass test data, test data predictions and test data probabilities 
# Example: test data             -> Y_test
#          test data predictions -> XGBC_pred
#          test data probabilities -> XGBC_Prob

# Calling functions
#   Eval_Metrics(Y_test,XGBC_pred,XGBC_prob)
#   M_Evaluation.auc_roc_plot(Y_test,XGBC_pred)  

def accuracy(y_test,y_pred_class):
    return (metrics.accuracy_score(y_test, y_pred_class))


def log_loss(y_test,y_pred_prob):
    loss=metrics.log_loss(y_test,y_pred_prob)
    return(loss)
    
def confusionmatrix(y_test,y_pred_class,correlationId,pageInfo,userId):
        confusion = confusion_matrix(y_test, y_pred_class)
        if confusion.shape==(1,1):
             e = "The Model Predicted has Only One Class Value Due To Class Imbalance. Please Apply Smote On The Data Transfomation Tab under Data Engineering and Retrain The Models."
             utils.updQdb(correlationId,'E',e,pageInfo,userId)
             return
        TP = confusion[1, 1]
        TN = confusion[0, 0]
        FP = confusion[0, 1]
        FN = confusion[1, 0]
        
        #TP = confusion[1, 1]
        #TN = confusion[0, 0]
        #FP = confusion[0, 1]
        #FN = confusion[1, 0]
        sensitivity = TP / float(FN + TP)
        specificity = TN / (TN + FP)
        #false_positive_rate = FP / float(TN + FP)
        precision = TP / float(TP + FP)
        recall    = TP / float(TP + FN)
        #print (precision,recall)
        f1score   =  2*((precision*recall)/(precision+recall))   
    
        return(confusion,TP,TN,FP,FN,sensitivity,specificity,precision,recall,f1score)
    

    


def classification_error(TP,TN,FP,FN):
    classification_error = (FP + FN) / float(TP + TN + FP + FN)
    return(classification_error)
    
    
def auc_roc_score(y_test, y_pred):
    score = metrics.roc_auc_score(y_test, y_pred)
    return(score)
    
def pred_prob(model,X_test):
    test_pred = model.predict(X_test)
    test_prob = model.predict_proba(X_test)
    return(test_pred, test_prob)

def plot_roc_curve(y_test, y_pred_prob, acc):
    try:
        fpr, tpr, thresholds = metrics.roc_curve(y_test, y_pred_prob)
    except ValueError:
        fpr, tpr, thresholds = metrics.roc_curve(y_test, y_pred_prob,pos_label=set(y_test).pop())
    #print (tpr,fpr)
    roc_auc = auc(fpr, tpr)
    
    if acc > 0.5 and roc_auc < 0.5:
        roc_auc = 1-roc_auc
        
        plt.plot(1-fpr, 1-tpr, color='orange', label='ROC curve (area = %0.2f)' % roc_auc)
        plt.plot([0, 1], [0, 1], color='darkblue', linestyle='--')
        plt.xlabel('False Positive Rate')
        plt.ylabel('True Positive Rate')
        plt.title('Receiver Operating Characteristic (ROC) Curve')
        plt.legend(loc="lower right")
    else:
    #print ("tpr,fpr",len(fpr),len(tpr))
        plt.plot(fpr, tpr, color='orange', label='ROC curve (area = %0.2f)' % roc_auc)
    
        plt.plot([0, 1], [0, 1], color='darkblue', linestyle='--')
        plt.xlabel('False Positive Rate')
        plt.ylabel('True Positive Rate')
        plt.title('Receiver Operating Characteristic (ROC) Curve')
        plt.legend(loc="lower right")
    with tempfile.TemporaryFile(suffix=".png") as tmpfile:
        plt.savefig(tmpfile,format="png")
        tmpfile.seek(0)
        aucEncoded = str(base64.b64encode(tmpfile.read()))
        
    plt.close()
    return roc_auc,aucEncoded

def Eval_Metrics(Y_test,test_pred,test_prob,model,X_test,correlationId,pageInfo,userId):   
    from evaluation import M_Evaluation
    #test_prob1 = [each[1] for each in test_prob]
    #test_prob2 = [each[Y_test[indx]] for indx,each in enumerate(test_prob)]   
    test_pred,test_prob=pred_prob(model,X_test)
    accuracy = M_Evaluation.accuracy(Y_test,test_pred)
    
    #print (Y_test,test_pred)
    #log_loss = M_Evaluation.log_loss(Y_test,test_prob[:,1])
    log_loss=0
    
    confusion,TP,TN,FP,FN,sensitivity,specificity,precision,recall,f1score = confusionmatrix(Y_test,test_pred,correlationId,pageInfo,userId)
    #if TP > TN:
    #    test_prob1 = [each[0] for each in test_prob]
    #else:
    test_prob1 = [each[1] for each in test_prob]
    
    '''if TN > TP:
        test_prob1 = [each[0] for each in test_prob]
    else:
        test_prob1 = [each[1] for each in test_prob]'''
    
    
    c_error = M_Evaluation.classification_error(TP,TN,FP,FN)
    
   # ar_score = M_Evaluation.auc_roc_score(Y_test,test_prob[:,1])
    #ar_score = M_Evaluation.auc_roc_score(Y_test,test_prob1)
    #print ("Y_test test pred",len(Y_test),len(test_pred),len(test_prob))
    #print ("\ntesting auc score",ar_score)
    #print ("\nTP,TN,FP,FN",TP,TN,FP,FN)
    #if isnan(f1score)
    if np.isnan(f1score):
        f1score="Undefined"
    ar_score,aucEncoded = plot_roc_curve(Y_test, test_prob1, accuracy)
    #print ("\nimage : ",aucEncoded)
    if accuracy==0:
        accuracy = (float(TP+TN))/(TP+TN+FP+FN)
    return accuracy,log_loss,TP,TN,FP,FN,sensitivity,specificity,precision,recall,f1score,c_error,ar_score,aucEncoded



'''
from sklearn.metrics import roc_curve, auc
    #y_pred_keras = model.predict(x_test).ravel()
fpr_keras, tpr_keras, thresholds_keras = roc_curve(Y_test, test_pr)
auc_keras = auc(fpr_keras, tpr_keras)
    '''

    
    
    
    
    
