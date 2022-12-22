import subprocess
import sys
import flair
data=sys.argv[1]
import os 
dir_path = os.path.dirname(os.path.realpath(__file__))
mydata={}
try:
	model_path=dir_path+'/sentiment-en-mix-distillbert_4.pt'
	flair_sentiment=flair.models.TextClassifier.load(model_path)
	s = flair.data.Sentence(data)
	flair_sentiment.predict(s)
	total_sentiment = s.labels
	val=str(total_sentiment[0]).split(' ')[-2]
	mydata['Output']=val
	mydata['Error']='No error'
	print(mydata)
except Exception as e:
	mydata['Error']=str(e)
	mydata['Output']='Has error, couldnot process'
	print(mydata)
	
