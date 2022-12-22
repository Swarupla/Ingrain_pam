import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'app-prediction-donut',
  templateUrl: './prediction-donut.component.html',
  styleUrls: ['./prediction-donut.component.scss']
})
export class PredictionDonutComponent implements OnInit {
  @Input() pythonProgressVal;
  @Input() label;
  @Input() donutInnerRadius;

  constructor() { }

  ngOnInit() {
  }

}
