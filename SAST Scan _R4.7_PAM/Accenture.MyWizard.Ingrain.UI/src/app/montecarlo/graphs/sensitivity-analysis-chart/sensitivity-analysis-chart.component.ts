import { Component, OnInit, AfterViewInit, ViewChild, ElementRef, Input, OnChanges } from '@angular/core';
import * as d3 from 'd3';


@Component({
  selector: 'app-sensitivity-analysis-chart',
  templateUrl: './sensitivity-analysis-chart.component.html',
  styleUrls: ['./sensitivity-analysis-chart.component.scss']
})
export class SensitivityAnalysisChartComponent implements OnInit, AfterViewInit, OnChanges {

  @ViewChild('SensitivityAnalysisChart', { static: true }) containerBarChart: ElementRef;
  htmlElement: HTMLElement;
  host;
  svg;
  _horizontalBarchartWidth;
  @Input() horizontalBarchartHeight: number;
  @Input() horizontalBarchartWidth: number;
  @Input() data: Array<any>;
  @Input() scenarioName: string;
  margin: any = { top: 0, bottom: 20, left: 60, right: 20 };
  parsedData = [];
  problemType: string;
  constructor() { }

  ngOnInit() {
  }

  ngOnChanges(changes) {
    this.createChart();
    this.problemType = this.scenarioName;
  }

  ngAfterViewInit() {
    this.createChart();
    this.problemType = this.scenarioName;
  }

  createChart() {
    const _this = this;
    const oldWidth = this.horizontalBarchartWidth + 50;
    const oldHeight = this.horizontalBarchartHeight;

    const element = this.containerBarChart.nativeElement;
    element.innerText = '';

    const Names = Object.keys(_this.data);
    const Values = Object.values(_this.data);
    const Colors = ['rgba(38, 125, 178, 1)'];

    const textLength = [];

    for (let index in Names) {
      textLength.push(Names[index].length);
      this.parsedData.push({ 'text': Names[index], 'value': Values[index], 'color': Colors[index] });
    }

    const sumOfTxtLength = textLength.reduce((a, b) => a + b, 0);

    const margin = {
      top: 20,
      right: 55,
      bottom: 25,
      left: 60
    };

    const width = oldWidth - margin.left - margin.right,
      height = oldHeight - margin.top - margin.bottom;

    const svg = d3.select(element).append('svg')
      .attr('width', width + margin.left + margin.right)
      .attr('height', height + margin.top + margin.bottom)
      .append('g')
      .attr('transform', 'translate(' + (margin.left) + ',0)');

    const yLabelText = d3.select(element).append('div')
      .attr('id', 'yFullText')
      // .style('opacity', 0)
      .style('display', 'none')
      .attr('class', 'tooltip')
      .style('font-size', '0.7rem');

    const x = d3.scaleLinear()
      .range([0, width])
      .domain([0, d3.max(this.parsedData, function (d) {
        return d.value;
      })])
      .nice();

    const y = d3.scaleBand()
      .rangeRound([height, 0])
      .padding(0.5)
      .domain(this.parsedData.map(function (d) {
        return d.text;
      }));

    // make y axis to show bar names
    const yAxis = d3.axisLeft()
      .scale(y)
      // no tick marks
      .tickSize(0);

    const gy = svg.append('g')
      .attr('class', 'y-axis-text')
      .attr('font-size', '12px')
      .call(yAxis);

    const wrapText = _this.wrapWordEllipse;
    svg.selectAll('.y-axis-text text')
      .call(wrapText, 30);

    gy.select('.domain')
      .attr('stroke-width', 0);

    const bars = svg.selectAll('.bar')
      .data(this.parsedData)
      .enter()
      .append('g');

    // append rects
    bars.append('rect')
      .attr('class', 'bar')
      .attr('y', function (d) {
        return y(d.text);
      })
      .attr('height', y.bandwidth())
      .attr('x', 0)
      .attr('width', function (d) {
        return x(d.value);
      })
      .attr('fill', function (d) {
        return 'rgba(38, 125, 178, 1)';
      })
      .on('mouseover', d => {
        const target = d3.event.currentTarget;
        const coords = [(target.getBBox().x + 100), target.getBBox().y];
        this.showFullText(d, this.scenarioName, coords, sumOfTxtLength);
      })
      .on('mouseleave', this.hideTooltip);


    // add a value label to the right of each bar
    bars.append('text')
      .attr('class', 'label')
      // y position of the label is halfway down the bar
      .attr('y', function (d) {
        return y(d.text) + y.bandwidth() / 2 + 4;
      })
      // x position is 3 pixels to the right of the bar
      .attr('x', function (d) {
        return x(d.value) + 3;
      })
      .attr('font-size', 12)
      .attr('font-family', 'sans-serif')
      .text(function (d) {
        return d.value.toFixed(2) + '%';
      });

    // add the x Axis
    svg.append('g')
      .attr('transform', 'translate(0,' + height + ')')
      .classed('xAxis', true)
      .call(d3.axisBottom(x));

    // Add axis labels
    svg.append('text')
      .attr('class', 'x label')
      .attr('transform', 'translate(' + (width / 2) + ' ,' + (height + this.margin.bottom - this.margin.top) + ')')
      .attr('text-anchor', 'middle')
      .attr('dy', '1.8em')
      .style('font-size', '0.8rem')
      .text('Sensitivity(R-squared)');

  }

  wrapWordEllipse(label, width) {
    label.each(function () {
      let text = d3.select(this);
      text = text._groups[0][0];
      if (text.textContent.length > 10) {
        text.textContent = text.textContent.substring(0, 5) + '..';
      }
    });
  }

  showFullText(d, problemType, coords, sumOfTxtLength) {
    const tooltipDiv = d3.select('#yFullText');
    tooltipDiv.transition().duration(100).style('opacity', 1);
    tooltipDiv.html(d.text);
    tooltipDiv.style('display', 'block');
    if (problemType === 'Generic') {
      if (sumOfTxtLength < 80) {
        tooltipDiv.style('left', coords[0] + 10 + 'px');
        tooltipDiv.style('top', coords[1] + 70 + 'px');
      } else {
        tooltipDiv.style('left', coords[0] + 10 + 'px');
        tooltipDiv.style('top', coords[1] + 90 + 'px');
      }
    } else if (problemType === 'ADSP') {
      tooltipDiv.style('left', coords[0] + 10 + 'px');
      tooltipDiv.style('top', coords[1] + 70 + 'px');
    }
  }

  hideTooltip(d) {
    const tooltipDiv = d3.select('#yFullText');
    // tooltipDiv.transition().duration(100)
    //  .style('visibility', 'hidden');
    // .style('opacity', 0);
    tooltipDiv.style('display', 'none');
  }

  moveTooltip(d, problemType) {
    const tooltipDiv = d3.select('#yFullText');
    // tooltipDiv
    // // .style('left', (d3.mouse(this)[0] + 20) + 'px')  
    // .style('top', (d3.mouse(this)[1]) + 'px');
    // .style('top', (d3.event.pageY - 30) + 'px').style('left', (d3.event.pageX + 30) + 'px');   
    if (problemType === 'Generic') {
      tooltipDiv.style('top', (d3.event.pageY - 70) + 'px');
    } else if (problemType === 'ADSP') {
      tooltipDiv.style('top', (d3.event.y - 70) + 'px');
    }
  }

}
