import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'app-frequency-line-chart',
  templateUrl: './frequency-line-chart.component.html',
  styleUrls: ['./frequency-line-chart.component.scss']
})
export class FrequencyLineChartComponent implements OnInit {

  @Input() isData;
  @Input() screenWidth;
  @Input() frequencyType;
  @Input() lineChartDataCount;
  @Input() selectedModelLineChart;

  constructor() { }

  ngOnInit() {
  }

}
