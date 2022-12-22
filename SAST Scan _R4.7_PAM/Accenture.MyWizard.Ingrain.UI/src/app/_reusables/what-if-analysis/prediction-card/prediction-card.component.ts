import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'app-prediction-card',
  templateUrl: './prediction-card.component.html',
  styleUrls: ['./prediction-card.component.scss']
})
export class PredictionCardComponent implements OnInit {

  @Input() predictions;
  @Input() problemType;
  @Input() prescriptiveAnalysis;
  @Input() targetColumn;

  constructor() { }

  ngOnInit() {
  }

}
