import { AfterViewInit, Component, ElementRef, Input, OnInit, ViewChild } from '@angular/core';
import * as d3 from 'd3';

@Component({
  selector: 'app-ad-bar-chart',
  templateUrl: './ad-bar-chart.component.html',
  styleUrls: ['./ad-bar-chart.component.scss']
})

export class AdBarChartComponent implements OnInit, AfterViewInit {
  @ViewChild('containerBarChart', { static: true }) chartContainer: ElementRef;
  @Input('barData') public anomalyData: any = [];
  @Input('modelName') public modelName;
  @Input('ChartWidth') public ChartWidth: number;
  @Input('ChartHeight') public ChartHeight: number;

  svg;
  margin = 10;
  width: number;
  height: number;
  barWidth: Number = 4;
  textFont: string = '7px';
  legendTextFont: string = '5px';
  strokeColor: string = 'lightgrey';
  strokeWidth: string = '0.5';
  strokeOpacity: string = '0.7';
  legendRectWidth : string = '10px';
  legendRectHeight : string = '4px';
  chartData = [];
  pageSize: number = 30;
  pageNumber: number = 1;
  dataCopy = [];
  legendSvg;

  legend = [{ name: 'Anomaly', color: 'red' }, { name: 'Non Anomaly', color: 'blue' }];

  constructor() {
  }

  ngOnInit(): void {
    this.dataCopy = this.anomalyData;
    this.width = this.ChartWidth - this.margin * 2;
    this.height = this.ChartHeight - this.margin * 2;

    // below code, which is for pagination.
    for (var i = 0; i < this.pageSize; i++) {
      this.chartData.push(this.anomalyData[i]);
    }

    this.createLegendSvg();
  }

  ngAfterViewInit(): void {
    this.createSvg();
    this.drawBars(this.chartData);// for pagination.
    //this.drawBars(this.anomalyData);
  }

  private createSvg(): void {
    this.svg = d3
      .select('div#chart')
      .append('svg')
      .attr(
        'viewBox',
        `-10 0 ${this.width + this.margin * 2} ${this.height + this.margin * 2}`
      )

      .append('g')
      .attr('transform', 'translate(' + this.margin + ',' + this.margin + ')');
  }

  private createLegendSvg(): void {
    this.legendSvg = d3
      .select('div#legend')
      .append('svg')
      .attr(
        'viewBox',
        `0 0 ${this.width + this.margin * 2} ${this.margin + 5}`
      )

      .append('g')
      .attr('transform', 'translate(' + this.margin + ',' + this.margin + ')');
      this.addLegend();
  }

  private drawBars(data: any[]): void {
    
    // Creating X-axis band scale
    const x = d3
      .scaleBand()
      .range([0, this.width])
      .domain(data.map((d) => d.key + d.Xaxis))
      .padding(0.2);

    // Drawing X-axis on the DOM
    this.svg
      .append('g')
      .attr("class", "x axis")
      .attr('transform', 'translate(0,' + this.height + ')')
      .call(d3.axisBottom(x).tickSizeOuter(0).tickSize(0).tickPadding(2).tickSizeInner(-this.height))
      .selectAll('text')
      //.attr('transform', 'translate(-10, 0)rotate(-45)')// rotate x axis lables
      .style('text-anchor', 'middle')
      .style('font-size', this.textFont);

    // Creaate Y-axis band scale
    const y = d3
      .scaleLinear()
      //.domain([0, Number(this.highestValue)])
      .domain([0, d3.max(data, (d) =>d.Yaxis)])
      .range([this.height, 0]);

    // Draw the Y-axis on the DOM
    this.svg
      .append('g')
      .call(d3.axisLeft(y).tickSizeOuter(0).tickSize(0).tickPadding(2).tickSizeInner(-this.width))
      .selectAll('text')
      .style('font-size', this.textFont);

    this.updateBar(data, x, y);
  }

  private updateBar(data, x, y) {
    // Create and fill the bars
    this.svg
      .selectAll('bars')
      .data(data)
      .enter()
      .append('rect')
      // .attr('x', (d) => {
      //   const val = x(d.key + d.Xaxis);
      //   return val ? + val + 2 : 0
      // })
      .attr('x', (d) => x(d.key + d.Xaxis))
      .attr('y', (d) => y(d.Yaxis))
      .attr('width', x.bandwidth())
      .attr('height', (d) =>{
        if (d.Yaxis == 0) {
          return 0;
        } else {
          return y(d.Yaxis) < this.height ? this.height - y(d.Yaxis) : this.height
        }
      }) // this.height
      .attr('fill', (d) => { return (d.Xaxis > 0) ? this.legend[1].color : this.legend[0].color });

    //this.addHeaderFooter();
    this.addGridStyles();
  }

  private addHeaderFooter() {
    // adding header title of the chart
    this.svg.append("text")
      .attr("x", this.width / 2)
      .attr("y", -5)
      .attr("text-anchor", "middle")
      .style("font-size", this.legendTextFont)
      .style("font-weight", "bold")
      .text(this.modelName + ' Predicted Output');

    // adding footer title of the chart
    this.svg.append("text")
      .attr("x", this.width / 2)
      .attr("y", this.height + 10)
      .attr("text-anchor", "middle")
      .style("font-size", this.legendTextFont)
      .style("font-weight", "bold")
      .text(this.modelName);

  }

  private addLegend() {
    var legend = this.legendSvg.append("g")
      .attr("class", "legend")
      .attr("x", 0)
      .attr("y", 0)
      .attr("height", 2)
      .attr("width", 2)
      .style("border", '1px');

    legend.append("rect")
      .attr("x", 0)
      .attr("y", -10)
      .attr("width", this.legendRectWidth)
      .attr("height", this.legendRectHeight)
      .style("fill", this.legend[0].color);

    legend.append("text")
      .attr("x", 12)
      .attr("y", -5)
      .text(this.legend[0].name)
      .style('font-size', this.textFont);

    legend.append("rect")
      .attr("x", 0)
      .attr("y", -2)
      .attr("width", this.legendRectWidth)
      .attr("height", this.legendRectHeight)
      .style("fill", this.legend[1].color);


    legend.append("text")
      .attr("x", 12)
      .attr("y", 2)
      .text(this.legend[1].name)
      .style('font-size', this.textFont);

  }

  private addGridStyles() {
    this.svg.selectAll('path').attr('stroke', this.strokeColor).attr('stroke-width', this.strokeWidth).attr('opacity', this.strokeOpacity);
    this.svg.selectAll('line').attr('stroke', this.strokeColor).attr('stroke-width', this.strokeWidth).attr('opacity', this.strokeOpacity);
  }

  // Pagination event to get page no and update chart data.
  public changePage($event) {
    this.pageNumber = $event;
    this.chartData = this.dataCopy.slice((this.pageNumber - 1) * this.pageSize, this.pageNumber * this.pageSize);
    this.deleteElementIfAlready();
    this.createSvg();
    this.drawBars(this.chartData);
  }

  //method to remove old chart data.
  private deleteElementIfAlready() {
    let element = this.chartContainer.nativeElement;
    if (element.hasChildNodes()) {
      let eleLength = element.childNodes.length;
      do {
        const childNodes = Array.from(element.childNodes);

        Array.prototype.forEach.call(childNodes, function (childNode) {
          childNode.remove();
        });

        eleLength = element.childNodes.length;
      } while (eleLength > 0);
    }
  }

}
