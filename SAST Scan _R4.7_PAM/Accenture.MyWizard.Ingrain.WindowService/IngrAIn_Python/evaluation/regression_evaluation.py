import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
from sklearn.metrics import mean_squared_error
from sklearn.metrics import mean_absolute_error
from sklearn.metrics import mean_squared_error
from sklearn.metrics import r2_score

from math import sqrt
'''
mean square error
multioutput :Defines aggregating of multiple output scores.
    Array-like value defines weights used to average scores.
    Default is “uniform_average”.
'''
def mse(y_true, y_pred, multioutput=None):
    if not multioutput:
        error = mean_squared_error(y_true, y_pred)
    else:
        error = mean_squared_error(y_true, y_pred,multioutput=multioutput)
    return {"error_rate":max(0,error)}
'''
mean absolute error
'''
def mae(y_true, y_pred, multioutput=None):
    if not multioutput:
        error = mean_absolute_error(y_true, y_pred)
    else:
        error = mean_absolute_error(y_true, y_pred,multioutput=multioutput)
    return {"error_rate":max(0,error)}
'''
root mean sqaure error
'''

def rms(y_true, y_pred, multioutput=None):
    error = mse(y_true, y_pred, multioutput=multioutput)
    error = sqrt(error["error_rate"])
    return {"error_rate":max(0,error)}

'''ajdusted root mean square error
'''
def r2score(y_true, y_pred, multioutput=None):
    if not multioutput:
        error = r2_score(y_true, y_pred)
    else:
        error = r2_score(y_true, y_pred,multioutput=multioutput)
    return {"error_rate":max(0,error)}

def evaluate_reg(y_true, y_pred, multioutput=None,timeseries=False):
    
    rmsVal = rms(y_true, y_pred, multioutput=None)
    maeVal = mae(y_true, y_pred, multioutput=None)
    mseVal = mse(y_true, y_pred, multioutput=None)
    #print("HHHHHHHHH",mseVal)
    mseVal["error_rate"] = '{:.3g}'.format(mseVal["error_rate"])
    if not timeseries:
        r2ScoreVal = r2score(y_true, y_pred, multioutput=None)
    else:
       # print ("here",rmsVal)
        r2ScoreVal =rmsVal.copy()
        #print (y_true)
        if max(y_true)!=min(y_true):
            r2ScoreVal["error_rate"] =1 - min(max(0,(r2ScoreVal["error_rate"]/(max(y_true)-min(y_true)))),1)
        else:
           r2ScoreVal["error_rate"]=0 
    return r2ScoreVal,rmsVal,maeVal,mseVal
    
    
                
