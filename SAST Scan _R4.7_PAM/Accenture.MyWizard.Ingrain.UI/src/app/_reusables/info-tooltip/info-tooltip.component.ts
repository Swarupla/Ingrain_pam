import { Component, OnInit, ViewChild, Input, ElementRef } from '@angular/core';

@Component({
  selector: 'app-info-tooltip',
  templateUrl: './info-tooltip.component.html',
  styleUrls: ['./info-tooltip.component.scss']
})
export class InfoTooltipComponent implements OnInit {
  @ViewChild('showpopover', { static: false }) showpopover;
  @Input() labelTitle: string;
  @Input() description: string; // use for staticDescriptions list
  @Input() isRequired: Boolean;
  @Input() isImage: Boolean = false;
  @Input() isImage1: Boolean = false;
  @Input() className;
  @Input() dynamicDescription; // Use as input if description value comes from api
  staticDescriptions: {};
  @Input() singleUpload;

  constructor() { }

  ngOnInit() {

    this.staticDescriptions = {

      'stopwords': `Stop words are very common words like “we”, “the”, “are”, etc. which probably do not help in NLP tasks. Hence, 
      we can remove stop words as it's presence does not add any context to the text data`,

      'mostfrequent': `Removes the selected percentage of most frequent words from the corpus.
    Example: if 10 percent is selected and total number of unique word counts is 100, then top 10 most frequent words will be removed.`,

      'leastfrequent': `Removes the selected percentage of least frequent words from the corpus.
    Example: if 10 percent is selected and total number of unique word counts is 100, then top 10 least frequent words will be removed.`,

      'normalization': `Helps us to achieve the root/base forms of inflected(derived) words.
    Example: For word 'troubling', for Lemmatization, base word will be 'trouble' and for Stemming, base word will be 'troubl'.`,

      'ngram': `A set of co-occurring words in the corpus within a given window/range.
    Example: If sentence is 'Hi it's a good day',
    for n-grams (1,2), co-occurring set of words will be [Hi, it's, a, good, day, Hi it's, it's a, a good, good day]
    for n-grams (2,2), co-occurring set of words will be [Hi it's, it's a, a good, good day]`,

      'generation': 'Perquisite method required to transform text data to numerical features.',

      'cluster': `Word level clustering of the numerical features, obtained post feature generation step.
    If number of clusters selected is 1, the optimal number of clusters will be auto determined and same 
    can be seen in the graph generated once the changes are applied.
    If number of clusters selected is not 1, then the user selected number will be considered as the optimal number of clusters.`,
      'pidatahelp': '<img src="assets/images/pi-data-help-icon.png" title="help" alt="help"/>',
      'aggregation': 'Method to aggregate and reduce the text generated features',
      'featureselection100percentage': ' The Model will select random 20% data from the given data set for Testing.',
      'featureSelectionTrainData': '% Split of the entire dataset that would be considered to train a model using various algorithms',
      'featureSelectionTestData': 'Remaining % of the entire dataset (100-Training Data%) that would be considered to test and evaluate the model created',
      'featureSelectionKFold': 'Technique by which a given data set is split into K number of sections/folds. A single fold is then treated as the test data and remaining folds are used as training data. Avoid k being too large or too small, causing bias in the model or long running time. Choose K such that each train/test group of data samples is sizable to the total dataset Eg: Suppose the dataset has 100 data points and k-fold is 5, then the dataset is divided into 5 sections/folds with 20 data points each and one-fold is considered as test data and the remaining 4 are considered as training data.',
      'featureSelectionStratifiedSampling': 'Sampling method where the population is partitioned into homogenous sub-population groups when data is split between training and test data. While splitting, it is often chosen to ensure that the train and test sets have approximately the same percentage of samples of each target class as the complete set. If there is a scenario that a sample has chances of falling into multiple classes, it is advised not to use Stratified Sampling',
      'defineproblemhstatement': 'Description of the business use-case user intends to address by creating this model',
      'attributetopredict': 'The Target Attribute of a dataset is the feature of a dataset about which user wants to gain insights',
      'uniqueidentifier': 'The Unique Attribute of the dataset is the feature that can uniquely identify each record',
      'potentialinfluencingattributes': 'The Input Attributes are those features which the user wants the model to take into consideration for gaining insights about Target Attribute. Predictive models use historical data to learn patterns and uncover relationships between input attributes of the dataset and the target attribute',
      'deploymodel': '1.	Public: Model can be viewed by the owner as well as other users having access to the selected Client & DC in which the model was created. Editing rights remain only with the owner. Other users can modify the features in Teach and Test section only to perform What-if Analysis and Hyper Tuning.' +
        ' 2.	Private: Model can be viewed and edited only by the owner. It will not be visible to other users.' +
        ' 3.	Model Template: Model can be consumed as a Template by all the users across ingrAIn. Editing rights of the deployed model template remain with the owner',
      'AUC-ROC-Curve': `AUC – ROC curve is a performance measurement for classification models. ROC is a probability curve and the AUC stands for Area under the curve. The larger the area coverage under the curve, the better is the fit of the model. In case of an excellent model the value is near to 1 which means it has good measure of separability. And when AUC is 0.5, it means model has no class separation capacity.  
     The ROC curve is plotted with True Positive Rate (TPR) against the False Positive Rate (FPR) where TPR is on y-axis and FPR is on the x-axis.
     Here, TPR = TP/ TP + FN
                FPR = FP/ TN + FP
     `,
      'F1Score': `F1- Score is a measure of a Classification model’s accuracy on a dataset and is defined as the harmonic mean of the model’s precision and recall. 
     Where, Precision = True Positives/ (True Positive + False Positive)
                   Recall (Sensitivity) = True Positives/ (True Positives + False Negative)
     A perfect model has an F1-Score of 1.
     `,
      'ConfusionMatrix': `: A confusion matrix is a table that is used to describe the performance of a classification model on a set of test data for which the true values are known.
     Here,
     o	True Positives (TP): These are cases in which we predicted High Priority, and they do have High Priority.
     o	True Negatives (TN): We predicted Low Priority, and they do have Low Priority.
     o	False Positives (FP): We predicted High Priority, but they are actually Low Priority. (Also known as a "Type I error.")
     o	False Negatives (FN): We predicted Low Priority, but they are actually High Priority. (Also known as a “Type II error.”)
     `,
      'r2value': `R2 is the measure of accuracy that represents the amount of variation in the target attribute that is accounted for by the input attributes. Usually, the larger the R2, the better the regression model fits the observations. 
                  Eg: If R2 is 0.77, 77% of the changes in the target attribute can be explained by the input attributes / It shows that 77% of the variance in the target attribute can be explained by the predictions
                  `,
      'msevalue': `Mean Square Error is the average of the square of the errors. Error in this case means the difference between the actual and the predicted values. The smaller the means squared error, the closer you are to finding the line of best fit.`,
      'matthewsCofficient': `Matthew’s Coefficient is a correlation coefficient that gives a measure of the quality of binary classification. It ranges from –1 to +1 where -1 means a completely wrong classifier whereas +1 indicates a completely correct classifier`,
      'timeseries': 'Time Series Forecasting is used to predict future values based on observed past values. You can set the value for frequency based on the intervals at which you need the prediction.',
      'Accuracy': "Accuracy refers to the closeness of the predicted value to the actual value. For Regression, it is measured using R-squared value and for Classification using F1 score. A change of +15% in the new model's accuracy from the previous accuracy will trigger auto retrain",
      'inputdrift': 'It describes the drift in mean of the newly ingested data from mean of the current data. A change of +/-20% between the means will be considered as unhealthy and auto retrain would be triggered',
      'targetvariance': 'Variance is the estimate of the change in target attribute when new data is ingested. For Regression, the drift in variance of the target attribute is calculated. A change of +/-15% will trigger auto retraining. For Classification, any introduction of a new class in the data will trigger retraining.',
      'dataquality': 'Data Quality are computed based on percentage of missing values, outliers, balance and skewness of the data. This score is shown for the individual columns of the newly ingested data. If the average data quality score of the complete data set is less than 60% then model will throw a warning. But no auto re-training will happen for bad quality data.',
      'featureSelectionCascadingToggle': `The control can be used to enable/disable the model to be selected as part of Custom Cascade Model workflow.
	    Custom Cascade Model workflow - The process of including the target of one model as one of the influencer while training a separate model.
     `,
      'samplepayload': `- Sample of API response will be like 
     { "":"", 
   .....  
   ..  
    "TotalRecordCount":"1000", 
    "TotalPageCount":"", 
    "PageNumber":"", 
    "ActualData":[]
     } - Sample API request 
     { ....., ( User has given parameter in body) 
    "StartDate":"", 
    "EndDate":"", 
    "PageNumber":""
     }`,
      'silhouetteCoefficient': `The silhouette score is a measure of how similar an object is to its own cluster (cohesion)
  compared to other clusters (separation). The silhouette score ranges from −1 to +1, where a high value indicates that the
  object is well matched to its own cluster and poorly matched to neighboring clusters.`,
  'maximumworddescription': 'For better output, while providing the value for maximum number of words in the summary, please make sure to provide lesser value than that of the number of words provided as input.',
  'uniqueidinfo': 'Multiple or Bulk prediction will not be enabled unless Unique ID is selected.',
   'AI-Ngram': 'Select n-gram combination to represent cluster names. If (2,4) is selected, clusters can have 2 or 3 or 4 words in the cluster names',
   'pidatahelpDataSet': '<img src="assets/images/pi-data-help-icon.png" title="help" alt="help"/>' };
  }

  closePopover() {
    // console.log(this.className);

    this.showpopover.nativeElement.classList.remove('show');
  }

  openHelper() {
    // this.topValueForTooltip = i;
    this.showpopover.nativeElement.classList.add('show');
  }

  // getStylesforTooltip() {
  //   const top = this.topValueForTooltip + 210 * this.topValueForTooltip + '%';
  //   return top;
  // }


}
