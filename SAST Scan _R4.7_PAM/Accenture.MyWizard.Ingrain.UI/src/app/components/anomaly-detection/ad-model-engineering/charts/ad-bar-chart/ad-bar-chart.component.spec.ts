import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdBarChartComponent } from './ad-bar-chart.component';

describe('AdBarChartComponent', () => {
  let component: AdBarChartComponent;
  let fixture: ComponentFixture<AdBarChartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ AdBarChartComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AdBarChartComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
