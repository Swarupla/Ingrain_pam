# -*- coding: utf-8 -*-
"""
Created on Tue Jul 23 10:24:06 2019

@author: harsh.nandedkar
"""
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
from sklearn.metrics import confusion_matrix
from sklearn.metrics import classification_report
from sklearn.metrics import matthews_corrcoef
import pandas as pd
import seaborn as sn
import tempfile
import base64
from sklearn import metrics
import matplotlib.pyplot as plt
import collections
import numbers

    
def confusionmatrix(y_test,y_pred,unique_target):
    n = 100
    if len(list(set(y_test))) >= n:
        confusion = confusion_matrix(y_test[:n], y_pred[:n])
        unique_target = unique_target[:len(confusion)]
    else:
        confusion = confusion_matrix(y_test, y_pred)
    #confusion = confusion_matrix(y_test, y_pred)
    df_cm = pd.DataFrame(confusion,unique_target,unique_target)
    plt.figure(figsize = (10,7))
    sn.set(font_scale=1.4)#for label size
    image=sn.heatmap(df_cm, annot=True,cmap='Blues',fmt='g')
    image.set(xlabel='Predicted', ylabel='Actual')
    #imageEncoded=str(base64.b64encode(image.data))
    
    with tempfile.TemporaryFile(suffix=".png") as tmpfile:
        fig = image.get_figure()
        fig.savefig(tmpfile,format="png",bbox_inches='tight')
        tmpfile.seek(0)
        imageEncoded = str(base64.b64encode(tmpfile.read()))
    
    return confusion,imageEncoded

def accuracy(y_test,y_pred_class):
    accuracy_score=metrics.accuracy_score(y_test, y_pred_class)
    return accuracy_score

def mathews_coeff(y_test,y_pred):
    matthews_coefficient=matthews_corrcoef(y_test, y_pred)
    return matthews_coefficient



def formatfloat(x):
    return "%.3g" % float(x)

def pformat(dictionary, function):
    if isinstance(dictionary, dict):
        return type(dictionary)((key, pformat(value, function)) for key, value in dictionary.items())
    if isinstance(dictionary, collections.Container):
        return type(dictionary)(pformat(value, function) for value in dictionary)
    if isinstance(dictionary, numbers.Number):
        return function(dictionary)
    return dictionary


def classif_report(y_test,y_pred,unique_target):
    report=classification_report(y_test, y_pred,target_names=unique_target, output_dict=True)
    #print(classification_report(y_test, y_pred, target_names=unique_target))
    del_report=['macro avg','weighted avg']
    for key in del_report:
        del report[key]
    report = pformat(report,formatfloat)
    return report


def main(y_test,y_pred,unique_target):
    
    confusion,imageEncoded=confusionmatrix(y_test,y_pred,unique_target)
    accuracy_score=accuracy(y_test,y_pred)
    matthews_coefficient=mathews_coeff(y_test,y_pred)
    report=classif_report(y_test,y_pred,unique_target)
    
    return confusion,imageEncoded,accuracy_score,matthews_coefficient,report


def main_Text_Classification(y_test,y_pred):
    
    
    accuracy_score=accuracy(y_test,y_pred)
    matthews_coefficient=mathews_coeff(y_test,y_pred)
    
    return accuracy_score,matthews_coefficient
    