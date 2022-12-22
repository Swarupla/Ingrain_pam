import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DataEncodingComponent } from './data-encoding.component';

describe('DataEncodingComponent', () => {
  let component: DataEncodingComponent;
  let fixture: ComponentFixture<DataEncodingComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ DataEncodingComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DataEncodingComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
